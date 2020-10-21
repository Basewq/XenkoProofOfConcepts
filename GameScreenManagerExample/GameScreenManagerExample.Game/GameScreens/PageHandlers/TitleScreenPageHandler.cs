using GameScreenManagerExample.UI;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.UI.Controls;
using Stride.UI.Events;
using System;

namespace GameScreenManagerExample.GameScreens.PageHandlers
{
    public class TitleScreenPageHandler : PageHandlerBase
    {
        internal static readonly UIElementKey<Button> NewGameButton = new UIElementKey<Button>("NewGameButton");
        internal static readonly UIElementKey<Button> LoadGameButton = new UIElementKey<Button>("LoadGameButton");
        internal static readonly UIElementKey<Button> OptionsButton = new UIElementKey<Button>("OptionsButton");
        internal static readonly UIElementKey<Button> QuitButton = new UIElementKey<Button>("QuitButton");

        private bool _ignoreInputEvents;
        private UrlReference<Prefab>[] _subScreenUIUrls;
        private int _titleMenuActiveButtonIndex = 0;

        protected override void OnInitialize()
        {
            UIComponent.GetUI(NewGameButton).Click += CreateTitleMenuScreenTransitionClickHandler(0);
            UIComponent.GetUI(LoadGameButton).Click += CreateTitleMenuScreenTransitionClickHandler(1);
            UIComponent.GetUI(OptionsButton).Click += CreateTitleMenuScreenTransitionClickHandler(2);
            UIComponent.GetUI(QuitButton).Click += (sender, e) =>
            {
                Game.Exit();
            };

            _subScreenUIUrls = new[]
            {
                UIManager.NewGameScreenUIUrl,
                UIManager.LoadGameScreenUIUrl,
                UIManager.OptionsScreenUIUrl,
            };
        }

        private EventHandler<RoutedEventArgs> CreateTitleMenuScreenTransitionClickHandler(int? menuButtonIndex)
        {
            return (sender, e) =>
            {
                if (_ignoreInputEvents || !IsTopMostScreen)
                {
                    return;
                }
                NavigateScreenForward(menuButtonIndex);
            };
        }

        private async void NavigateScreenForward(int? menuButtonIndex)
        {
            _ignoreInputEvents = true;

            int sceneUrlIndex = menuButtonIndex ?? _titleMenuActiveButtonIndex;
            var nextSceenUrl = _subScreenUIUrls[sceneUrlIndex];
            //var uiSoundMgr = GameManagerEntity.Get<UISoundManager>();
            //uiSoundMgr?.PlaySound(uiSoundMgr.ButtonMainConfirmSoundEffect);
            var uiPageEntity = await UIManager.LoadUIEntityAsync(nextSceenUrl);
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
