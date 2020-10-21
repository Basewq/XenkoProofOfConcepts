using GameScreenManagerExample.UI;
using Stride.Engine;
using Stride.UI.Controls;
using System.Diagnostics;

namespace GameScreenManagerExample.GameScreens.PageHandlers
{
    public class OptionsScreenPageHandler : PageHandlerBase
    {
        internal static readonly UIElementKey<Button> BackButton = new UIElementKey<Button>("BackButton");

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
