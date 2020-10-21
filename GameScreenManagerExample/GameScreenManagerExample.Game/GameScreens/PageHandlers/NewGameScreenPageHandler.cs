namespace GameScreenManagerExample.GameScreens.PageHandlers
{
    public class NewGameScreenPageHandler : PageHandlerBase
    {
        public override async void OnActivate()
        {
            // Should actually have options, eg. difficulty selection, character selection, etc

            var scene = await SceneManager.LoadNextMainScene(SceneManager.InGameSceneUrl);
            SceneManager.SetAsActiveMainScene(scene);
        }
    }
}
