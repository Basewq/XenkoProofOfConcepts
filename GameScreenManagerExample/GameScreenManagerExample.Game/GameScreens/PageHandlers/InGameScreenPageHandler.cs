using GameScreenManagerExample.UI;
using Stride.Engine;
using Stride.Input;
using Stride.UI.Controls;
using System.Threading.Tasks;

namespace GameScreenManagerExample.GameScreens.PageHandlers
{
    public class InGameScreenPageHandler : PageHandlerBase
    {
        internal static readonly UIElementKey<TextBlock> CoinCounterText = new UIElementKey<TextBlock>("CoinCounterText");

        private TextBlock _coinsCounterText;

        private InputManager _inputManager;
        private bool _ignoreInputEvents;
        private Task _loadOptionsScreenTask;

        protected override void OnInitialize()
        {
            _inputManager = Game.Services.GetService<InputManager>();
            _coinsCounterText = UIComponent.GetUI(CoinCounterText);

            GameManager.CoinCollectedChanged += OnCoinCollectedChanged;
        }

        private void OnCoinCollectedChanged(int coinCount)
        {
            _coinsCounterText.Text = $"Coins Collected: {coinCount}";
        }

        protected override void OnDeinitialize()
        {
            GameManager.CoinCollectedChanged -= OnCoinCollectedChanged;
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
            if (_ignoreInputEvents || !IsTopMostScreen)
            {
                return;
            }
            if (_inputManager.HasKeyboard)
            {
                if (_loadOptionsScreenTask == null && _inputManager.IsKeyPressed(Keys.Escape))
                {
                    _loadOptionsScreenTask = LoadOptionsScreen();
                }
            }
        }

        private async Task LoadOptionsScreen()
        {
            var uiPageEntity = await UIManager.LoadUIEntityAsync(UIManager.InGameOptionsScreenUIUrl);
            UIManager.PushScreen(uiPageEntity);
            _loadOptionsScreenTask = null;
        }
    }
}
