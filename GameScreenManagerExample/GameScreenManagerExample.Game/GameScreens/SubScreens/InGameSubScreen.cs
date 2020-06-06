using GameScreenManagerExample.UI;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Input;
using Stride.UI.Controls;

namespace GameScreenManagerExample.GameScreens.SubScreens
{
    public class InGameSubScreen : SubScreenBase
    {
        internal static readonly UIElementKey<TextBlock> CoinCounterText = new UIElementKey<TextBlock>("CoinCounterText");

        private int _coinCollected = 0;
        private TextBlock _coinsCounterText;

        private InputManager _inputManager;
        private bool _ignoreInputEvents;

        [Display(10, "In-Game Options Sub-Screen")]
        public UrlReference<Scene> InGameOptionsSubScreenSceneUrl;

        protected override void OnInitialize()
        {
            _inputManager = Game.Services.GetService<InputManager>();

            _coinsCounterText = UIComponent.GetUI(CoinCounterText);
        }

        public override void OnActivate()
        {
            _ignoreInputEvents = false;
            _coinCollected = 0;
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
            if (_ignoreInputEvents || !IsTopMostScreen)
            {
                return;
            }
            if (_inputManager.HasKeyboard)
            {
                if (_inputManager.IsKeyPressed(Keys.Escape))
                {
                    GameScreenManager.LoadNextGameScreen(InGameOptionsSubScreenSceneUrl, onLoadCompleted: scene =>
                    {
                        GameScreenManager.PushSubScreen(scene);
                    });
                }
            }
        }

        internal void OnCoinCollected()
        {
            _coinCollected++;
            _coinsCounterText.Text = $"Coins Collected: {_coinCollected}";
        }
    }
}
