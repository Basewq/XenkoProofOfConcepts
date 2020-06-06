using GameScreenManagerExample.UI;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.UI.Controls;
using Stride.UI.Events;
using System;

namespace GameScreenManagerExample.GameScreens.SubScreens
{
    public class TitleInitialSubScreen : SubScreenBase
    {
        internal static readonly UIElementKey<Button> NewGameButton = new UIElementKey<Button>("NewGameButton");
        internal static readonly UIElementKey<Button> LoadGameButton = new UIElementKey<Button>("LoadGameButton");
        internal static readonly UIElementKey<Button> OptionsButton = new UIElementKey<Button>("OptionsButton");
        internal static readonly UIElementKey<Button> QuitButton = new UIElementKey<Button>("QuitButton");

        private bool _ignoreInputEvents;
        private UrlReference<Scene>[] _subScreenSceneUrls;
        private int _titleMenuActiveButtonIndex = 0;

        [Display(10, "New Game Sub-Screen")]
        public UrlReference<Scene> NewGameSubScreenSceneUrl;

        [Display(20, "Load Game Sub-Screen")]
        public UrlReference<Scene> ContinueGameSubScreenSceneUrl;

        [Display(30, "Options Sub-Screen")]
        public UrlReference<Scene> OptionsSubScreenSceneUrl;

        protected override void OnInitialize()
        {
            UIComponent.GetUI(NewGameButton).Click += CreateTitleMenuScreenTransitionClickHandler(0);
            UIComponent.GetUI(LoadGameButton).Click += CreateTitleMenuScreenTransitionClickHandler(1);
            UIComponent.GetUI(OptionsButton).Click += CreateTitleMenuScreenTransitionClickHandler(2);
            UIComponent.GetUI(QuitButton).Click += (sender, e) =>
            {
                Game.Exit();
            };

            _subScreenSceneUrls = new[]
            {
                NewGameSubScreenSceneUrl,
                ContinueGameSubScreenSceneUrl,
                OptionsSubScreenSceneUrl,
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

        private void NavigateScreenForward(int? menuButtonIndex)
        {
            _ignoreInputEvents = true;

            int sceneUrlIndex = menuButtonIndex ?? _titleMenuActiveButtonIndex;
            var nextSceneUrl = _subScreenSceneUrls[sceneUrlIndex];
            //var uiSoundMgr = GameManagerEntity.Get<UISoundManager>();
            //uiSoundMgr?.PlaySound(uiSoundMgr.ButtonMainConfirmSoundEffect);
            GameScreenManager.LoadNextGameScreen(nextSceneUrl, onLoadCompleted: scene =>
            {
                GameScreenManager.PushSubScreen(scene);
            });
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
