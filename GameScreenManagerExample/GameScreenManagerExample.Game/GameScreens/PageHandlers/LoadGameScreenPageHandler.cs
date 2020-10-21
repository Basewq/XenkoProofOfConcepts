using GameScreenManagerExample.UI;
using Stride.Engine;
using Stride.UI.Controls;
using System.Diagnostics;

namespace GameScreenManagerExample.GameScreens.PageHandlers
{
    public class LoadGameScreenPageHandler : PageHandlerBase
    {
        internal static readonly UIElementKey<Button> BackButton = new UIElementKey<Button>("BackButton");
        internal static readonly UIElementKey<Button> ContinueButton = new UIElementKey<Button>("ContinueButton");

        private bool _ignoreInputEvents;

        protected override void OnInitialize()
        {
            UIComponent.GetUI(BackButton).Click += (sender, e) =>
            {
                if (_ignoreInputEvents)
                {
                    return;
                }

                _ignoreInputEvents = true;
                Debug.Assert(this == UIManager.TopPageHandler);
                UIManager.PopTopScreen();
            };
            UIComponent.GetUI(ContinueButton).Click += async (sender, e) =>
            {
                if (_ignoreInputEvents)
                {
                    return;
                }

                _ignoreInputEvents = true;
                Debug.Assert(this == UIManager.TopPageHandler);
                var scene = await SceneManager.LoadNextMainScene(SceneManager.InGameSceneUrl);
                // Should reload save game data, and apply this data to the scene before setting it as the active scene
                SceneManager.SetAsActiveMainScene(scene);
            };
        }

        public override void OnActivate()
        {
            _ignoreInputEvents = false;
        }

        protected override void OnIsTopMostScreenChanged(bool newIsTopMostScreen)
        {
            if (newIsTopMostScreen)
            {
                _ignoreInputEvents = false;
            }
        }
    }
}
