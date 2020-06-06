using GameScreenManagerExample.UI;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.UI.Controls;
using System.Diagnostics;

namespace GameScreenManagerExample.GameScreens.SubScreens
{
    public class LoadGameSubScreen : SubScreenBase
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
                Debug.Assert(this == GameScreenManager.ActiveSubScreen);
                GameScreenManager.PopSubScreen();
            };
            UIComponent.GetUI(ContinueButton).Click += (sender, e) =>
            {
                if (_ignoreInputEvents)
                {
                    return;
                }

                _ignoreInputEvents = true;
                Debug.Assert(this == GameScreenManager.ActiveSubScreen);
                GameScreenManager.LoadNextGameScreen(GameScreenManager.InGameScreenSceneUrl, scene =>
                {
                    // Should reload save game data, and apply this data to the scene before setting it as the active scene
                    GameScreenManager.SetAsActiveGameScreen(scene);
                });
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
