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

        public async Task ConnectActivate(string playerName, string serverIp, ushort serverPortNumber)
        {
            _currentConnectionState = ConnectionState.Connecting;
            ConnectionStateChanged?.Invoke(_currentConnectionState);

            Task<Scene> loadSceneTask;
            if (string.IsNullOrEmpty(serverIp))
            {
                // Single player mode
                _networkService.IsEnabled = false;
                _currentConnectionState = ConnectionState.CanEnterGame;

                loadSceneTask = SceneManager.LoadSceneAsync(SceneManager.InGameSceneUrl);
            }
            else
            {
                // Connect to server
                _networkService.IsEnabled = true;

                var connectTask = _networkService.BeginConnectToServer(serverIp, serverPortNumber);
                var connectResult = await connectTask;
                if (!connectResult.IsOk)
                {
                    ConnectionError?.Invoke(connectResult.ErrorMessage);
                    return;
                }

                // Request to join the game
                _currentConnectionState = ConnectionState.JoinGameRequest;
                ConnectionStateChanged?.Invoke(_currentConnectionState);
                var joinGameTask = _networkService.Client_SendJoinGameRequest(playerName);
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
                var syncClockTask = _networkService.Client_SendClockSynchronization();
                var syncClockResult = await syncClockTask;
                if (!syncClockResult.IsOk)
                {
                    ConnectionError?.Invoke(syncClockResult.ErrorMessage);
                    return;
                }

                _currentConnectionState = ConnectionState.CanEnterGame;
                ConnectionStateChanged?.Invoke(_currentConnectionState);
            }

            var scene = await loadSceneTask;
            if (loadSceneTask.IsFaulted)
            {
                ConnectionError?.Invoke("Level could not be loaded.");
                return;
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

    public enum ConnectionState
    {
        Idle,
        Connecting,
        JoinGameRequest,
        SynchronizingClock,
        CanEnterGame,
    }
}
