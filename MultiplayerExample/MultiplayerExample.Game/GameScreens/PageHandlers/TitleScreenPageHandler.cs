using MultiplayerExample.GameServices.SceneHandlers;
using MultiplayerExample.UI;
using Stride.Engine;
using Stride.UI.Controls;
using Stride.UI.Events;
using System;

namespace MultiplayerExample.GameScreens.PageHandlers
{
    public class TitleScreenPageHandler : PageHandlerBase
    {
        internal static readonly UIElementKey<Button> SinglePlayerGameButton = new UIElementKey<Button>("SinglePlayerGameButton");
        internal static readonly UIElementKey<Button> MultiplayerHostGameButton = new UIElementKey<Button>("MultiplayerHostGameButton");
        internal static readonly UIElementKey<Button> MultiplayerJoinGameButton = new UIElementKey<Button>("MultiplayerJoinGameButton");
        internal static readonly UIElementKey<EditText> PlayerNameEditText = new UIElementKey<EditText>("PlayerNameEditText");
        internal static readonly UIElementKey<EditText> ServerIpEditText = new UIElementKey<EditText>("ServerIpEditText");
        internal static readonly UIElementKey<EditText> ServerPortNumberEditText = new UIElementKey<EditText>("ServerPortNumberEditText");
        internal static readonly UIElementKey<Button> QuitButton = new UIElementKey<Button>("QuitButton");

        private bool _ignoreInputEvents;
        //private int _titleMenuActiveButtonIndex = 0;  // TODO: should be used for controller support, ignored for this proof of concept

        private EditText _playerNameText;
        private EditText _serverIpText;
        private EditText _serverPortNumberText;

        protected override void OnInitialize()
        {
            _playerNameText = UIComponent.GetUI(PlayerNameEditText);
            _serverIpText = UIComponent.GetUI(ServerIpEditText);
            _serverPortNumberText = UIComponent.GetUI(ServerPortNumberEditText);
            _serverPortNumberText.Text = Engine.GameEngineServer.DefaultServerPortNumber.ToString();

            UIComponent.GetUI(SinglePlayerGameButton).Click += CreateTitleMenuScreenTransitionClickHandler(TitleScreenStartGameMode.SinglePlayerGame);
            UIComponent.GetUI(MultiplayerHostGameButton).Click += CreateTitleMenuScreenTransitionClickHandler(TitleScreenStartGameMode.HostMultiplayerGame);
            UIComponent.GetUI(MultiplayerJoinGameButton).Click += CreateTitleMenuScreenTransitionClickHandler(TitleScreenStartGameMode.JoinMultiplayerGame);
            UIComponent.GetUI(QuitButton).Click += (sender, e) =>
            {
                GameManager.ExitGame();
            };
        }

        private EventHandler<RoutedEventArgs> CreateTitleMenuScreenTransitionClickHandler(TitleScreenStartGameMode startGameMode)
        {
            return (sender, e) =>
            {
                if (_ignoreInputEvents || !IsTopMostScreen)
                {
                    return;
                }
                NavigateScreenForward(startGameMode);
            };
        }

        private async void NavigateScreenForward(TitleScreenStartGameMode startGameMode)
        {
            _ignoreInputEvents = true;

            //var uiSoundMgr = GameManagerEntity.Get<UISoundManager>();
            //uiSoundMgr?.PlaySound(uiSoundMgr.ButtonMainConfirmSoundEffect);

            var uiPageEntity = await UIManager.LoadUIEntityAsync(UIManager.ServerConnectionScreenUIUrl);
            var pageHandler = uiPageEntity.GetPageHandlerFromUIPageEntity<ServerConnectionScreenPageHandler>();
            pageHandler.StartGameMode = startGameMode;
            pageHandler.PlayerName = _playerNameText.Text;
            if (startGameMode == TitleScreenStartGameMode.HostMultiplayerGame || startGameMode == TitleScreenStartGameMode.JoinMultiplayerGame)
            {
                // TODO: should really do proper validation
                pageHandler.ServerIp = _serverIpText.Text;
                ushort.TryParse(_serverPortNumberText.Text, out pageHandler.ServerPortNumber);
            }
            else
            {
                pageHandler.ServerIp = null;
                pageHandler.ServerPortNumber = default;
            }

            UIManager.PushScreen(uiPageEntity);
        }

        protected override void OnDeinitialize()
        {
        }

        public override void OnActivate()
        {
            _ignoreInputEvents = false;
        }

        //public override void OnDeactivate() { }

        //public override void Update() { }

        //internal protected override void OnScreenSizeChanged(Vector2 screenSize) { }

        protected override void OnIsTopMostScreenChanged(bool newIsTopMostScreen)
        {
            if (newIsTopMostScreen)
            {
                _ignoreInputEvents = false;
            }
        }
    }
}
