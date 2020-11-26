using MultiplayerExample.GameServices;
using MultiplayerExample.GameServices.SceneHandlers;
using MultiplayerExample.UI;
using Stride.Core;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using System.Diagnostics;

namespace MultiplayerExample.GameScreens.PageHandlers
{
    /// <summary>
    /// Sub-screen used by the client when connecting to the server.
    /// </summary>
    public class ServerConnectionScreenPageHandler : PageHandlerBase
    {
        internal static readonly UIElementKey<TextBlock> ConnectionStatusText = new UIElementKey<TextBlock>("ConnectionStatusText");
        internal static readonly UIElementKey<UIElement> ConnectionErrorContent = new UIElementKey<UIElement>("ConnectionErrorContent");
        internal static readonly UIElementKey<TextBlock> ConnectionErrorText = new UIElementKey<TextBlock>("ConnectionErrorText");
        internal static readonly UIElementKey<Button> BackButton = new UIElementKey<Button>("BackButton");

        private Button _backButton;
        private TextBlock _connectionStatusText;
        private UIElement _connectionErrorContent;
        private TextBlock _connectionErrorText;

        private TitleScreenSceneHandler _sceneHandler;

        [DataMemberIgnore]
        internal string PlayerName;
        [DataMemberIgnore]
        internal string ServerIp;
        [DataMemberIgnore]
        internal ushort ServerPortNumber;

        protected override void OnInitialize()
        {
            _connectionStatusText = UIComponent.GetUI(ConnectionStatusText);
            _connectionErrorContent = UIComponent.GetUI(ConnectionErrorContent);
            _connectionErrorText = UIComponent.GetUI(ConnectionErrorText);
            _backButton = UIComponent.GetUI(BackButton);
            _backButton.Click += (sender, e) =>
            {
                Debug.Assert(UIManager.TopPageHandler == this);
                UIManager.PopTopScreen();
            };

            var sceneManager = SceneSystem.GetSceneManagerFromRootScene();
            _sceneHandler = sceneManager.ActiveMainSceneHandler as TitleScreenSceneHandler;
            Debug.Assert(_sceneHandler != null);
            _sceneHandler.ConnectionStateChanged += OnConnectionStateChanged;
            _sceneHandler.ConnectionError += OnConnectionError;
        }

        protected override void OnDeinitialize()
        {
            _sceneHandler.ConnectionStateChanged -= OnConnectionStateChanged;
            _sceneHandler.ConnectionError -= OnConnectionError;
            _sceneHandler = null;
        }

        private void OnConnectionStateChanged(ConnectionState connectionState)
        {
            switch (connectionState)
            {
                case ConnectionState.Idle:
                    _connectionStatusText.Text = "Idle";
                    break;
                case ConnectionState.Connecting:
                    _connectionStatusText.Text = "Connecting...";
                    break;
                case ConnectionState.JoinGameRequest:
                    _connectionStatusText.Text = "Joining server...";
                    break;
                case ConnectionState.SynchronizingClock:
                    _connectionStatusText.Text = "Syncing to server clock...";
                    break;
                case ConnectionState.CanEnterGame:
                    _connectionStatusText.Text = "Loading level...";
                    break;
            }
        }

        private void OnConnectionError(string errorMessage)
        {
            _connectionErrorContent.Visibility = Visibility.Visible;
            _connectionErrorText.Text = errorMessage;

            _connectionStatusText.Visibility = Visibility.Hidden;
            _backButton.Visibility = Visibility.Visible;
        }

        public async override void OnActivate()
        {
            _connectionErrorContent.Visibility = Visibility.Hidden;
            _connectionStatusText.Visibility = Visibility.Visible;
            _connectionStatusText.Text = "";
            _backButton.Visibility = Visibility.Collapsed;

            await _sceneHandler.BeginGameConnection(PlayerName, ServerIp, ServerPortNumber);
        }

        //public override void OnDeactivate()
        //{
        //}
    }
}
