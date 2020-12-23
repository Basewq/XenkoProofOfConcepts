using MultiplayerExample.Engine;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;
using Stride.Physics;
using System;
using System.Reflection;

namespace MultiplayerExample
{
    public class GameAppClient : Game, IExitGameService, IGame
    {
        private GameEngineClient _gameEngine;

        private bool _isFirstUpdate = true;

        private bool _isResizeTickEnabled;
        private System.Timers.Timer _resizeTickTimer;

        // Ensures scripts using UpdateTime/DrawTime in scripts use the ones in the engine
        GameTime IGame.DrawTime => _gameEngine.RenderTime;
        GameTime IGame.UpdateTime => _gameEngine.UpdateTime;

        public GameAppClient() : base()
        {
            Services.AddService<IExitGameService>(this);
#if DEBUG
            TreatNotFocusedLikeMinimized = false;     // Useful to set to false when testing multiple clients on the same machine
#endif
        }

        protected override void PrepareContext()
        {
            base.PrepareContext();

            var gameSettingsService = Services.GetSafeServiceAs<IGameSettingsService>();
            var gameSettings = gameSettingsService.Settings;
            var physicsSettings = gameSettings.Configurations.Get<PhysicsSettings>();
            // Ignore whatever was set in the config asset
            physicsSettings.Flags = PhysicsEngineFlags.ContinuousCollisionDetection;
            physicsSettings.MaxSubSteps = 0;    // Important to keep this at 0 since this makes BulletPhysics simulate exactly one step per update
            physicsSettings.FixedTimeStep = (float)GameConfig.PhysicsFixedTimeStep.TotalSeconds;
        }

        protected override void Initialize()
        {
            base.Initialize();

            _gameEngine = new GameEngineClient(Content, services: Services, GameSystems);
            _gameEngine.Initialize();
        }

        protected override void BeginRun()
        {
            _gameEngine.LoadContent();      // HACK: can't run in Initialize, need to run this in BeginRun because we need additional setup from BeginDraw call, but this happens after Initialize
            base.BeginRun();
        }

        protected override void Update(GameTime gameTime)
        {
            if (_isFirstUpdate)
            {
                GameSystems.Update(gameTime);   // HACK: need to force a first update flag inside this instance even if its empty
                _gameEngine.InitialUpdate();
                _isFirstUpdate = false;
                return;
            }

            // Don't call base.Update(gameTime); since this everything must be updated through _gameEngine
            _gameEngine.Update();
        }

        protected override bool BeginDraw()
        {
            bool canDraw = base.BeginDraw();
            canDraw = canDraw && _gameEngine.BeginDraw();
            return canDraw;
        }

        protected override void Draw(GameTime gameTime)
        {
            // From base.Draw()
            if (GraphicsDevice != null && GraphicsDevice.Presenter.BackBuffer != null)
            {
                GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);
            }

            // Our internal engine does the real rendering
            _gameEngine.Draw();
        }

        protected override void OnWindowCreated()
        {
            base.OnWindowCreated();

            // Adapted from https://github.com/MonoGame/MonoGame/pull/6594
            // This code allows the game to continue running while the user is dragging the window via the title bar.
            // Note that this solution only works for Winforms.
            var nativeWindowProperty = Window.NativeWindow.GetType().GetProperty("NativeWindow", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (nativeWindowProperty != null)
            {
                var gameFormObj = nativeWindowProperty.GetValue(Window.NativeWindow);
                if (gameFormObj != null)
                {
                    _resizeTickTimer = new System.Timers.Timer(1)
                    {
                        SynchronizingObject = gameFormObj as System.ComponentModel.ISynchronizeInvoke,
                        AutoReset = false
                    };
                    _resizeTickTimer.Elapsed += OnResizeTick;

                    //var resizeEvent = gameFormObj.GetType().GetEvent("Resize", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    var resizeBeginEvent = gameFormObj.GetType().GetEvent("ResizeBegin", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (resizeBeginEvent != null)
                    {
                        resizeBeginEvent.AddEventHandler(gameFormObj, (EventHandler)OnResizeBegin);
                    }
                    var resizeEndEvent = gameFormObj.GetType().GetEvent("ResizeEnd", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (resizeEndEvent != null)
                    {
                        resizeEndEvent.AddEventHandler(gameFormObj, (EventHandler)OnResizeEnd);
                    }
                }
            }
        }

        private void OnResizeBegin(object sender, EventArgs e)
        {
            _isResizeTickEnabled = true;
            _resizeTickTimer.Enabled = true;
        }

        private void OnResizeTick(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!_isResizeTickEnabled)
            {
                return;
            }

            Tick();
            _resizeTickTimer.Enabled = true;        // Allow the timer to call this event again
        }

        private void OnResizeEnd(object sender, EventArgs eventArgs)
        {
            _isResizeTickEnabled = false;
            _resizeTickTimer.Enabled = false;
        }
    }
}
