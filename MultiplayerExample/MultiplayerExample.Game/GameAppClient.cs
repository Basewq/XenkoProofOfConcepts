using MultiplayerExample.Engine;
using MultiplayerExample.Network;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;
using Stride.Input;
using Stride.Physics;
using Stride.Rendering.Fonts;
using Stride.Streaming;
using System;
using System.Linq;
using System.Reflection;

namespace MultiplayerExample
{
    public class GameAppClient : Game, IExitGameService, IGame
    {
        private NetworkAssetDatabase _networkAssetDatabase;

        private GameEngineClient _gameEngine;

        private readonly GameSystemKeyValue<StreamingManager> _streamingManager;
        private GameSystemKeyValue<InputManager> _inputManager;
        private GameSystemKeyValue<GameFontSystem> _gameFontSystem;

        private bool _isFirstUpdate = true;

        private bool _isResizeTickEnabled;
        private System.Timers.Timer _resizeTickTimer;

        // Ensures scripts using UpdateTime/DrawTime in scripts use the ones in the engine
        GameTime IGame.DrawTime => _gameEngine.RenderTime;
        GameTime IGame.UpdateTime => _gameEngine.UpdateTime;

        public GameAppClient() : base()
        {
            _streamingManager = CreateKeyValue(Streaming);

            Services.AddService<IExitGameService>(this);
#if DEBUG
            TreatNotFocusedLikeMinimized = false;     // Useful to set to false when testing multiple clients on the same machine
#endif
        }

        protected override void PrepareContext()
        {
            base.PrepareContext();

            _networkAssetDatabase = new NetworkAssetDatabase(Content, assetFolderUrls: new[] { "Prefabs", "Scenes" });
            Services.AddService(_networkAssetDatabase);

            var gameSettingsService = Services.GetSafeServiceAs<IGameSettingsService>();
            var gameSettings = gameSettingsService.Settings;
            var physicsSettings = gameSettings.Configurations.Get<PhysicsSettings>();
            // Ignore whatever was set in the config asset
            physicsSettings.Flags = PhysicsEngineFlags.ContinuousCollisionDetection;
            physicsSettings.MaxSubSteps = 0;    // Important to keep this at 0 since this makes BulletPhysics simulate exactly one step per update
            physicsSettings.FixedTimeStep = 1f / GameConfig.PhysicsSimulationRate;
        }

        protected override void Initialize()
        {
            base.Initialize();

            _streamingManager.System.Initialize();

            _inputManager = CreateKeyValue(Input);
            Input.VirtualButtonConfigSet = new VirtualButtonConfigSet();

            var gameFontSystem = GameSystems.First(x => x is GameFontSystem) as GameFontSystem;
            _gameFontSystem = CreateKeyValue(gameFontSystem);
            Services.AddService(gameFontSystem);

            _gameEngine = new GameEngineClient(Content, globalServices: Services);
            _gameEngine.Initialize();

            GameSystems.Clear();    // We do not use this GameSystems, (nearly) everything is done through _gameEngine
        }

        protected override void BeginRun()
        {
            ((IContentable)_gameFontSystem.System).LoadContent();
            _gameEngine.LoadContent();      // HACK: can't run in Initialize, need to run this in BeginRun because we need additional setup from BeginDraw call, but this happens after Initialize
            base.BeginRun();
        }

        protected override void Update(GameTime gameTime)
        {
            if (_isFirstUpdate)
            {
                GameSystems.Update(gameTime);   // HACK: need to force a first update flag inside this instance even if its empty
                _streamingManager.TryUpdate(gameTime);
                _inputManager.TryUpdate(gameTime);
                _gameEngine.InitialUpdate();
                _isFirstUpdate = false;
                return;
            }

            // Don't call base.Update(gameTime); since this everything must be updated through _gameEngine
            // (with the exception of a few global/shared systems.)
            _streamingManager.TryUpdate(gameTime);
            _inputManager.TryUpdate(gameTime);
            _gameFontSystem.TryUpdate(gameTime);
            // Note that the game engine class maintains its own GameTime object
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

        private static GameSystemKeyValue<T> CreateKeyValue<T>(T gameSystem) where T : GameSystemBase
        {
            var gameSystemKey = new ProfilingKey(GameProfilingKeys.GameUpdate, gameSystem.GetType().Name);
            return new GameSystemKeyValue<T>(gameSystemKey, gameSystem);
        }
    }
}
