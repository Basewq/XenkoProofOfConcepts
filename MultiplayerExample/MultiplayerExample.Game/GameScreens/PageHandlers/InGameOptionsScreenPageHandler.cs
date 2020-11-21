﻿using MultiplayerExample.UI;
using Stride.Engine;
using Stride.Input;
using Stride.UI.Controls;
using System.Diagnostics;

namespace MultiplayerExample.GameScreens.PageHandlers
{
    public class InGameOptionsScreenPageHandler : PageHandlerBase
    {
        internal static readonly UIElementKey<Button> ResumeButton = new UIElementKey<Button>("ResumeButton");
        internal static readonly UIElementKey<Button> MainMenuButton = new UIElementKey<Button>("MainMenuButton");
        internal static readonly UIElementKey<Button> QuitButton = new UIElementKey<Button>("QuitButton");

        private InputManager _inputManager;
        private bool _ignoreInputEvents;

        protected override void OnInitialize()
        {
            _inputManager = GameManager.Services.GetService<InputManager>();

            UIComponent.GetUI(ResumeButton).Click += (sender, e) =>
            {
                if (_ignoreInputEvents)
                {
                    return;
                }

                _ignoreInputEvents = true;
                Debug.Assert(this == UIManager.TopPageHandler);
                UIManager.PopTopScreen();
            };
            UIComponent.GetUI(MainMenuButton).Click += async (sender, e) =>
            {
                if (_ignoreInputEvents)
                {
                    return;
                }

                _ignoreInputEvents = true;
                Debug.Assert(this == UIManager.TopPageHandler);
                var scene = await SceneManager.LoadSceneAsync(SceneManager.TitleScreenSceneUrl);
                SceneManager.SetAsActiveMainScene(scene);
            };
            UIComponent.GetUI(QuitButton).Click += (sender, e) =>
            {
                GameManager.ExitGame();
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
                    Debug.Assert(this == UIManager.TopPageHandler);
                    UIManager.PopTopScreen();
                }
            }
        }
    }
}
