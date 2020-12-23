using MultiplayerExample.Camera;
using MultiplayerExample.Core;
using MultiplayerExample.Engine;
using MultiplayerExample.Network;
using Stride.Engine;
using System.Diagnostics;
using System.Linq;

namespace MultiplayerExample.GameServices.SceneHandlers
{
    public class InGameSceneHandler : SceneHandlerBase
    {
        private IGameNetworkClientHandler _networkClientHandler;

        internal InitialSceneSettings InitialSettings = default;

        protected override void OnInitialize()
        {
            Debug.WriteLine($"{nameof(InGameSceneHandler)} Initialize");

            if (GameManager.GameEngineContext.IsClient)
            {
                // Must deactivate the root camera before attaching
                var rootScene = SceneSystem.SceneInstance.RootScene;
                var mainCamEnt = rootScene.Entities.FirstOrDefault(x => x.Name == CameraExt.RootSceneMainCameraEntityName);
                mainCamEnt.Get<CameraComponent>().Enabled = false;
            }
        }

        public override async void OnActivate()
        {
            var networkService = GameManager.NetworkService;
            if (networkService.IsGameHost)
            {
                var serverOnlyDataScene = SceneManager.LoadSceneSync(SceneManager.InGameServerOnlyDataSceneUrl);    // Load this immediately
                serverOnlyDataScene.MergeSceneTo(Scene);

                var gameClockManager = GameManager.GameClockManager;
                gameClockManager.SimulationClock.Reset();
                gameClockManager.SimulationClock.IsEnabled = true;
            }
            if (GameManager.GameEngineContext.IsClient)
            {
                var uiPageEntity = await UIManager.LoadUIEntityAsync(UIManager.InGameScreenUIUrl);
                UIManager.SetAsMainScreen(uiPageEntity);
            }

            foreach (var proc in SceneSystem.SceneInstance.Processors)
            {
                if (proc is IInGameProcessor inGameProc)
                {
                    inGameProc.IsEnabled = true;
                }
            }

            switch (networkService.NetworkGameMode)
            {
                case NetworkGameMode.Local:
                case NetworkGameMode.ListenServer:
                    if (InitialSettings.AddLocalPlayer)
                    {
                        var networkServerHandler = networkService.GetServerHandler();
                        Debug.Assert(!string.IsNullOrEmpty(InitialSettings.LocalPlayerName));
                        networkServerHandler.CreateLocalPlayer(InitialSettings.LocalPlayerName);
                    }
                    break;
                case NetworkGameMode.RemoteClient:
                    {
                        _networkClientHandler = networkService.GetClientHandler();
                        var readyTask = _networkClientHandler.SendClientInGameReady();
                        var readyResult = await readyTask;
                        if (!readyResult.IsOk)
                        {
                            var scene = await SceneManager.LoadSceneAsync(SceneManager.TitleScreenSceneUrl);
                            // TODO: should ShowErrorMessage(readyResult.ErrorMessage);
                            SceneManager.SetAsActiveMainScene(scene);
                            return;
                        }
                        _networkClientHandler.Disconnected += OnNetworkClientDisconnected;
                        break;
                    }
            }
        }

        private void OnNetworkClientDisconnected()
        {
            var scene = SceneManager.LoadSceneSync(SceneManager.TitleScreenSceneUrl);
            // TODO: should ShowErrorMessage(readyResult.ErrorMessage);
            SceneManager.SetAsActiveMainScene(scene);
        }

        public override void OnDeactivate()
        {
            if (_networkClientHandler != null)
            {
                _networkClientHandler.Disconnected -= OnNetworkClientDisconnected;
            }

            if (GameManager.NetworkService.IsGameHost)
            {
                var gameClockManager = GameManager.GameClockManager;
                gameClockManager.SimulationClock.IsEnabled = false;
            }
            if (GameManager.GameEngineContext.IsClient)
            {
                // Reactivate the root camera before attaching
                var rootScene = SceneSystem.SceneInstance.RootScene;
                var mainCamEnt = rootScene.Entities.FirstOrDefault(x => x.Name == CameraExt.RootSceneMainCameraEntityName);
                mainCamEnt.Get<CameraComponent>().Enabled = true;
            }

            foreach (var proc in SceneSystem.SceneInstance.Processors)
            {
                if (proc is IInGameProcessor inGameProc)
                {
                    inGameProc.IsEnabled = false;
                }
            }
        }

        internal struct InitialSceneSettings
        {
            public bool AddLocalPlayer;
            public string LocalPlayerName;
        }
    }
}
