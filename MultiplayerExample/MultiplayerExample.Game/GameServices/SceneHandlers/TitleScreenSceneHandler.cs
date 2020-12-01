using MultiplayerExample.Network;
using Stride.Core;
using Stride.Engine;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MultiplayerExample.GameServices.SceneHandlers
{
    public class TitleScreenSceneHandler : SceneHandlerBase
    {
        private IGameNetworkService _networkService;
        private NetworkAssetDatabase _networkAssetDatabase;

        private Scene _inGameScene = null;
        private ConnectionState _currentConnectionState = ConnectionState.Idle;

        public event Action<ConnectionState> ConnectionStateChanged;
        public event Action<string> ConnectionError;

        protected override void OnInitialize()
        {
            Debug.WriteLine($"{nameof(TitleScreenSceneHandler)} Initialize");

            _networkService = GameManager.Services.GetSafeServiceAs<IGameNetworkService>();
            _networkAssetDatabase = GameManager.Services.GetSafeServiceAs<NetworkAssetDatabase>();
        }

        public override async void OnActivate()
        {
            var uiPageEntity = await UIManager.LoadUIEntityAsync(UIManager.TitleScreenUIUrl);
            UIManager.SetAsMainScreen(uiPageEntity);
        }

        public async Task BeginGameConnection(TitleScreenStartGameMode startGameMode, string playerName, string serverIp, ushort serverPortNumber)
        {
            Debug.Assert(startGameMode != TitleScreenStartGameMode.NotSet, $"{nameof(startGameMode)} was not set.");

            _currentConnectionState = ConnectionState.Connecting;
            ConnectionStateChanged?.Invoke(_currentConnectionState);

            Task<Scene> loadSceneTask = null;
            switch (startGameMode)
            {
                case TitleScreenStartGameMode.SinglePlayerGame:
                    {
                        _networkService.StartLocalGame();
                        _currentConnectionState = ConnectionState.CanEnterGame;
                        // Load the game scene in the background
                        var sceneUrl = SceneManager.InGameSceneUrl;
                        loadSceneTask = SceneManager.LoadSceneAsync(sceneUrl);
                    }
                    break;
                case TitleScreenStartGameMode.HostMultiplayerGame:
                    {
                        _networkService.StartHost(serverPortNumber);
                        _currentConnectionState = ConnectionState.CanEnterGame;
                        // Load the game scene in the background
                        var sceneUrl = SceneManager.InGameSceneUrl;
                        loadSceneTask = SceneManager.LoadSceneAsync(sceneUrl);
                    }
                    break;
                case TitleScreenStartGameMode.JoinMultiplayerGame:
                    {
                        Debug.Assert(!string.IsNullOrEmpty(playerName));
                        Debug.Assert(!string.IsNullOrEmpty(serverIp));
                        // Connect to server
                        var connectTask = _networkService.BeginConnectToServer(serverIp, serverPortNumber);
                        var connectResult = await connectTask;
                        if (!connectResult.IsOk)
                        {
                            ConnectionError?.Invoke(connectResult.ErrorMessage);
                            return;
                        }
                        var networkClientHandler = connectResult.ClientHandler;

                        // Request to join the game
                        _currentConnectionState = ConnectionState.JoinGameRequest;
                        ConnectionStateChanged?.Invoke(_currentConnectionState);
                        var joinGameTask = networkClientHandler.SendJoinGameRequest(playerName);
                        var joinGameResult = await joinGameTask;
                        if (!joinGameResult.IsOk)
                        {
                            ConnectionError?.Invoke(joinGameResult.ErrorMessage);
                            return;
                        }

                        // Load the game scene in the background
                        var sceneAssetId = joinGameResult.LoadSceneAssetId;
                        var sceneUrl = _networkAssetDatabase.GetUrlReferenceFromAssetId<Scene>(sceneAssetId);
                        loadSceneTask = SceneManager.LoadSceneAsync(sceneUrl);

                        // Synchronize the game clock
                        _currentConnectionState = ConnectionState.SynchronizingClock;
                        ConnectionStateChanged?.Invoke(_currentConnectionState);
                        var syncClockTask = networkClientHandler.SendClockSynchronization();
                        var syncClockResult = await syncClockTask;
                        if (!syncClockResult.IsOk)
                        {
                            ConnectionError?.Invoke(syncClockResult.ErrorMessage);
                            return;
                        }

                        _currentConnectionState = ConnectionState.CanEnterGame;
                        ConnectionStateChanged?.Invoke(_currentConnectionState);
                    }
                    break;
                default:
                    Debug.Fail($"Unhandled game mode: {startGameMode}");
                    break;
            }

            var scene = await loadSceneTask;
            if (loadSceneTask.IsFaulted)
            {
                ConnectionError?.Invoke("Level could not be loaded.");
                return;
            }

            var sceneHandler = scene.GetSceneHandlerFromScene<InGameSceneHandler>();
            switch (startGameMode)
            {
                case TitleScreenStartGameMode.SinglePlayerGame:
                case TitleScreenStartGameMode.HostMultiplayerGame:
                    {
                        sceneHandler.InitialSettings.AddLocalPlayer = true;
                        sceneHandler.InitialSettings.LocalPlayerName = playerName;
                    }
                    break;
                default:
                    sceneHandler.InitialSettings = default;
                    break;
            }
            // Store it in a field and wait for the next Update call to ensure we don't get into issues like
            // leaving the screen before the load completed.
            _inGameScene = scene;
        }

        public override void OnDeactivate()
        {
            _inGameScene = null;
        }

        public override void Update()
        {
            if (_currentConnectionState == ConnectionState.CanEnterGame
                && _inGameScene != null)
            {
                SceneManager.SetAsActiveMainScene(_inGameScene);

                _currentConnectionState = ConnectionState.Idle;
                _inGameScene = null;
                return;
            }
        }
    }

    public enum TitleScreenStartGameMode
    {
        NotSet,
        SinglePlayerGame,
        HostMultiplayerGame,
        JoinMultiplayerGame
    }

    public enum ConnectionState
    {
        Idle,
        Connecting,
        JoinGameRequest,
        SynchronizingClock,
        CanEnterGame,
    }
}
