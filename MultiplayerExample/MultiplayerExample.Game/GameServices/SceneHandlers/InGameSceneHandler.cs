using MultiplayerExample.Camera;
using MultiplayerExample.Engine;
using Stride.Engine;
using System.Diagnostics;
using System.Linq;

namespace MultiplayerExample.GameServices.SceneHandlers
{
    public class InGameSceneHandler : SceneHandlerBase
    {
        protected override void OnInitialize()
        {
            Debug.WriteLine($"{nameof(InGameSceneHandler)} Initialize");

            if (!GameManager.GameEngineContext.IsServer)
            {
                // Must deactivate the root camera before attaching
                var rootScene = SceneSystem.SceneInstance.RootScene;
                var mainCamEnt = rootScene.Entities.FirstOrDefault(x => x.Name == CameraExt.RootSceneMainCameraEntityName);
                mainCamEnt.Get<CameraComponent>().Enabled = false;
            }
        }

        public override async void OnActivate()
        {
            if (GameManager.GameEngineContext.IsServer)
            {
                var gameClockManager = GameManager.Services.GetService<GameClockManager>();
                gameClockManager.SimulationClock.Reset();
                gameClockManager.SimulationClock.IsEnabled = true;
            }
            if (!GameManager.GameEngineContext.IsServer)
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
        }

        public override void OnDeactivate()
        {
            if (GameManager.GameEngineContext.IsServer)
            {
                var gameClockManager = GameManager.Services.GetService<GameClockManager>();
                gameClockManager.SimulationClock.IsEnabled = false;
            }
            if (!GameManager.GameEngineContext.IsServer)
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
    }
}
