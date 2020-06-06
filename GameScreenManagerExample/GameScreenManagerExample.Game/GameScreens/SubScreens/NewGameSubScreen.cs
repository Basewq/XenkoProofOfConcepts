namespace GameScreenManagerExample.GameScreens.SubScreens
{
    public class NewGameSubScreen : SubScreenBase
    {
        public override void OnActivate()
        {
            GameScreenManager.LoadNextGameScreen(GameScreenManager.InGameScreenSceneUrl, scene =>
            {
                GameScreenManager.SetAsActiveGameScreen(scene);
            });
        }
    }
}
