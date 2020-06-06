using GameScreenManagerExample.UI;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using System.Diagnostics;

namespace GameScreenManagerExample.GameScreens
{
    public class SplashScreen : GameScreenBase
    {
        internal static readonly UIElementKey<UIElement> SplashContent1 = new UIElementKey<UIElement>("SplashContent1");
        internal static readonly UIElementKey<UIElement> SplashContent2 = new UIElementKey<UIElement>("SplashContent2");
        internal static readonly UIElementKey<TextBlock> LoadingText = new UIElementKey<TextBlock>("LoadingText");

        private FadeStep _currentFadeStep = FadeStep.FadeIn;
        private int _currentDisplayImageIndex = 0;
        private AnimationTimer _fadeTimer;

        private Scene _nextScene = null;

        private UIElement[] _splashScreenContents;
        private TextBlock _loadingText;

        protected override void OnInitialize()
        {
            Debug.WriteLine($"{nameof(SplashScreen)} Initialize");

            _splashScreenContents = new[]
            {
                UIComponent.GetUI(SplashContent1),
                UIComponent.GetUI(SplashContent2),
            };
            Debug.Assert(_splashScreenContents[0] != null, $"ImageElement named '{SplashContent1.UIName}' must be declared.");
            Debug.Assert(_splashScreenContents[1] != null, $"ImageElement named '{SplashContent2.UIName}' must be declared.");

            _loadingText = UIComponent.GetUI(LoadingText);
            Debug.Assert(_loadingText != null, $"TextBlock named '{LoadingText.UIName}' must be declared.");
        }

        public override void OnActivate()
        {
            Debug.WriteLine($"{nameof(SplashScreen)} OnActivate");

            _currentDisplayImageIndex = 0;

            _currentFadeStep = FadeStep.FadeIn;
            _fadeTimer = CreateFadeTimer(_currentFadeStep);

            for (int i = 0; i < _splashScreenContents.Length; i++)
            {
                _splashScreenContents[i].Opacity = 0;
            }
            _loadingText.Text = null;

            GameScreenManager.LoadNextGameScreen(GameScreenManager.TitleScreenSceneUrl, scene =>
            {
                _nextScene = scene;
            });
        }

        public override void OnDeactivate()
        {
            Debug.WriteLine($"{nameof(SplashScreen)} OnDeactivate");
        }

        public override void Update()
        {
#if DEBUG
            //CheckCanGoToNextScreen();
            //return;
#endif
            if (_currentFadeStep == FadeStep.Finished)
            {
                CheckCanGoToNextScreen();
                return;
            }

            if (_currentDisplayImageIndex < _splashScreenContents.Length)
            {
                float dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;
                _fadeTimer.Update(dt);
                var opacity = _currentFadeStep switch
                {
                    FadeStep.FadeIn => _fadeTimer.CompletionValueInDecimal * _fadeTimer.CompletionValueInDecimal,
                    FadeStep.Hold => 1,
                    FadeStep.FadeOut => (1 - _fadeTimer.CompletionValueInDecimal),
                    _ => 0,
                };
                _splashScreenContents[_currentDisplayImageIndex].Opacity = opacity;
                if (_fadeTimer.IsComplete)
                {
                    if (_currentFadeStep < FadeStep.Finished)
                    {
                        _currentFadeStep++;
                        if (_currentFadeStep == FadeStep.Finished)
                        {
                            _currentDisplayImageIndex++;
                            if (_currentDisplayImageIndex >= _splashScreenContents.Length)
                            {
                                _loadingText.Text = "Loading...";
                            }
                            else
                            {
                                _currentFadeStep = FadeStep.FadeIn;
                                _fadeTimer = CreateFadeTimer(_currentFadeStep);
                            }
                        }
                        else
                        {
                            _fadeTimer = CreateFadeTimer(_currentFadeStep);
                        }
                    }
                }
            }

            void CheckCanGoToNextScreen()
            {
                if (_nextScene != null)
                {
                    Debug.Assert(GameScreenManager.ActiveScreen == this);
                    GameScreenManager.SetAsActiveGameScreen(_nextScene);
                    _nextScene = null;
                }
            }
        }

        private static AnimationTimer CreateFadeTimer(FadeStep fadeStep)
        {
            var fadeTimer = fadeStep switch
            {
                FadeStep.FadeIn => new AnimationTimer(1.75f),
                FadeStep.Hold => new AnimationTimer(1.75f),
                FadeStep.FadeOut => new AnimationTimer(0.5f),
                _ => default,
            };
            return fadeTimer;
        }

        enum FadeStep
        {
            FadeIn,
            Hold,
            FadeOut,
            Finished
        }
    }
}
