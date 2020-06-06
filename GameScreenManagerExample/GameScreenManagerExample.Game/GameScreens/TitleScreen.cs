using Stride.Core;
using Stride.Core.Serialization;
using Stride.Engine;
using System.Diagnostics;

namespace GameScreenManagerExample.GameScreens
{
    public class TitleScreen : GameScreenBase
    {
        [Display(10, "Initial Sub-Screen")]
        public UrlReference<Scene> InitialSubScreenSceneUrl;

        protected override void OnInitialize()
        {
            Debug.WriteLine($"{nameof(TitleScreen)} Initialize");
        }

        public override void OnActivate()
        {
            GameScreenManager.LoadNextGameScreen(InitialSubScreenSceneUrl, scene =>
            {
                GameScreenManager.PushSubScreen(scene);
            });
        }
    }
}
