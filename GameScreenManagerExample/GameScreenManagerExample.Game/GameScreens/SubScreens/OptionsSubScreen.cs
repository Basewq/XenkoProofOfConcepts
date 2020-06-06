using GameScreenManagerExample.UI;
using Stride.Engine;
using Stride.UI.Controls;
using System.Diagnostics;

namespace GameScreenManagerExample.GameScreens.SubScreens
{
    public class OptionsSubScreen : SubScreenBase
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
                Debug.Assert(this == GameScreenManager.ActiveSubScreen);
                GameScreenManager.PopSubScreen();
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
