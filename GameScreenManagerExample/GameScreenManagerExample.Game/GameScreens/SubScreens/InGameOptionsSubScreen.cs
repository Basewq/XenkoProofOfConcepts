using GameScreenManagerExample.UI;
using Stride.Engine;
using Stride.Input;
using Stride.UI.Controls;
using System.Diagnostics;

namespace GameScreenManagerExample.GameScreens.SubScreens
{
    public class InGameOptionsSubScreen : SubScreenBase
    {
        internal static readonly UIElementKey<Button> ResumeButton = new UIElementKey<Button>("ResumeButton");
        internal static readonly UIElementKey<Button> MainMenuButton = new UIElementKey<Button>("MainMenuButton");
        internal static readonly UIElementKey<Button> QuitButton = new UIElementKey<Button>("QuitButton");

        private InputManager _inputManager;
        private bool _ignoreInputEvents;

        protected override void OnInitialize()
        {
            _inputManager = Game.Services.GetService<InputManager>();

            UIComponent.GetUI(ResumeButton).Click += (sender, e) =>
            {
                if (_ignoreInputEvents)
                {
                    return;
                }

                _ignoreInputEvents = true;
                Debug.Assert(this == GameScreenManager.ActiveSubScreen);
                GameScreenManager.PopSubScreen();
            };
            UIComponent.GetUI(MainMenuButton).Click += (sender, e) =>
            {
                if (_ignoreInputEvents)
                {
                    return;
                }

                _ignoreInputEvents = true;
                Debug.Assert(this == GameScreenManager.ActiveSubScreen);
                GameScreenManager.LoadNextGameScreen(GameScreenManager.TitleScreenSceneUrl, scene =>
                {
                    GameScreenManager.SetAsActiveGameScreen(scene);
                });
            };
            UIComponent.GetUI(QuitButton).Click += (sender, e) =>
            {
                Game.Exit();
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

        public override void Update()
        {
            if (_inputManager.HasKeyboard)
            {
                if (_inputManager.IsKeyPressed(Keys.Escape))
                {
                    Debug.Assert(this == GameScreenManager.ActiveSubScreen);
                    GameScreenManager.PopSubScreen();
                }
            }
        }
    }
}
