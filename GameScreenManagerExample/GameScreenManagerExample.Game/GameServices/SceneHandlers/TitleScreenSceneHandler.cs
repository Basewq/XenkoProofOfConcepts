using System.Diagnostics;

namespace GameScreenManagerExample.GameServices.SceneHandlers
{
    public class TitleScreenSceneHandler : SceneHandlerBase
    {
        protected override void OnInitialize()
        {
            Debug.WriteLine($"{nameof(TitleScreenSceneHandler)} Initialize");
        }

        public override async void OnActivate()
        {
            var uiPageEntity = await UIManager.LoadUIEntityAsync(UIManager.TitleScreenUIUrl);
            UIManager.SetAsMainScreen(uiPageEntity);
        }
    }
}
