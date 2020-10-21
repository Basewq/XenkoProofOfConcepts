using GameScreenManagerExample.GameScreens.PageHandlers;
using Stride.Engine;
using System.Diagnostics;

namespace GameScreenManagerExample.GameServices.SceneHandlers
{
    public class SplashScreenSceneHandler : SceneHandlerBase
    {
        private SplashScreenPageHandler _splashScreenPageHandler;

        private Scene _nextScene = null;

        public override async void OnActivate()
        {
            Debug.WriteLine($"{nameof(SplashScreenSceneHandler)} OnActivate");

            var loadUIPageEntityTask = UIManager.LoadUIEntityAsync(UIManager.SplashScreenUIUrl);
            var loadSceneTask = SceneManager.LoadNextMainScene(SceneManager.TitleScreenSceneUrl);

            var uiPageEntity = await loadUIPageEntityTask;
            UIManager.SetAsMainScreen(uiPageEntity);
            _splashScreenPageHandler = (SplashScreenPageHandler)UIManager.TopPageHandler;

            _nextScene = await loadSceneTask;
        }

        public override void OnDeactivate()
        {
            Debug.WriteLine($"{nameof(SplashScreenSceneHandler)} OnDeactivate");
            _splashScreenPageHandler = null;
            _nextScene = null;
        }

        public override void Update()
        {
            if (_splashScreenPageHandler?.IsAnimationFinished ?? false && _nextScene != null)
            {
                Debug.Assert(SceneManager.ActiveMainSceneHandler == this);
                SceneManager.SetAsActiveMainScene(_nextScene);
                _nextScene = null;
            }
        }
    }
}
