using GameScreenManagerExample.Cameras;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Engine;
using System.Diagnostics;
using System.Linq;

namespace GameScreenManagerExample.GameScreens
{
    public class InGameScreen : GameScreenBase
    {
        private Scene _gameScene;

        [Display(100, "In-Game Sub-Screen")]
        public UrlReference<Scene> InGameSubScreenSceneUrl;

        protected override void OnInitialize()
        {
            Debug.WriteLine($"{nameof(InGameScreen)} Initialize");
        }

        public override void OnActivate()
        {
            UIComponent.Enabled = true;
            GameScreenManager.LoadNextGameScreen(InGameSubScreenSceneUrl, scene =>
            {
                UIComponent.Enabled = false;
                _gameScene = scene;
                // Must deactivate the root camera before attaching
                var rootScene = SceneSystem.SceneInstance.RootScene;
                var mainCamEnt = rootScene.Entities.FirstOrDefault(x => x.Name == CameraExt.RootSceneMainCameraEntityName);
                mainCamEnt.Get<CameraComponent>().Enabled = false;

                GameScreenManager.PushSubScreen(scene);
            });
        }

        public override void OnDeactivate()
        {
            _gameScene = null;

            // Reactivate the root camera before attaching
            var rootScene = SceneSystem.SceneInstance.RootScene;
            var mainCamEnt = rootScene.Entities.FirstOrDefault(x => x.Name == CameraExt.RootSceneMainCameraEntityName);
            mainCamEnt.Get<CameraComponent>().Enabled = true;

        }
    }
}
