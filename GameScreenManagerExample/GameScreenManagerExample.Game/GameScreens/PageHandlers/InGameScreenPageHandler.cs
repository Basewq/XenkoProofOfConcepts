using GameScreenManagerExample.UI;
using Stride.Engine;
using Stride.Input;
using Stride.UI.Controls;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GameScreenManagerExample.GameScreens.PageHandlers
{
    public class InGameScreenPageHandler : PageHandlerBase
    {
        internal static readonly UIElementKey<TextBlock> CoinCounterText = new UIElementKey<TextBlock>("CoinCounterText");

        private TextBlock _coinsCounterText;

        private InputManager _inputManager;
        private bool _ignoreInputEvents;

        private readonly object _syncRoot = new object();
        private Task _loadOptionsScreenTask;
        private Task<Entity> _loadUIEntityTask;

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
            if (_loadOptionsScreenTask?.IsFaulted ?? false)
            {
                // Should probably log to a file.
                Debug.WriteLine("Error loading Options Screen UI: " + _loadOptionsScreenTask.Exception.ToString());
            }
            if (_ignoreInputEvents || !IsTopMostScreen)
            {
                return;
            }
            if (_inputManager.HasKeyboard && _inputManager.IsKeyPressed(Keys.Escape))
            {
                lock (_syncRoot)
                {
                    if (_loadUIEntityTask == null)
                    {
                        _loadOptionsScreenTask = LoadOptionsScreen();
                    }
                }
            }
        }

        private async Task LoadOptionsScreen()
        {
            _loadUIEntityTask = UIManager.LoadUIEntityAsync(UIManager.InGameOptionsScreenUIUrl);
            var uiPageEntity = await _loadUIEntityTask;
            lock (_syncRoot)
            {
                UIManager.PushScreen(uiPageEntity);
                _loadUIEntityTask = null;
            }
        }
    }
}
