using GameScreenManagerExample.Cameras;
using Stride.Engine;
using System.Diagnostics;
using System.Linq;

namespace GameScreenManagerExample.GameServices.SceneHandlers
{
    public class InGameSceneHandler : SceneHandlerBase
    {
        protected override void OnInitialize()
        {
            Debug.WriteLine($"{nameof(InGameSceneHandler)} Initialize");

            // Must deactivate the root camera before attaching
            var rootScene = SceneSystem.SceneInstance.RootScene;
            var mainCamEnt = rootScene.Entities.FirstOrDefault(x => x.Name == CameraExt.RootSceneMainCameraEntityName);
            mainCamEnt.Get<CameraComponent>().Enabled = false;
        }

        public override async void OnActivate()
        {
            GameManager.ResetGameplayFields();
            var uiPageEntity = await UIManager.LoadUIEntityAsync(UIManager.InGameScreenUIUrl);
            UIManager.SetAsMainScreen(uiPageEntity);
        }

        public override void OnDeactivate()
        {
            // Reactivate the root camera before attaching
            var rootScene = SceneSystem.SceneInstance.RootScene;
            var mainCamEnt = rootScene.Entities.FirstOrDefault(x => x.Name == CameraExt.RootSceneMainCameraEntityName);
            mainCamEnt.Get<CameraComponent>().Enabled = true;
        }
    }
}
