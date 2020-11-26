using MultiplayerExample.Core;
using MultiplayerExample.GameServices;
using MultiplayerExample.Network;
using Stride.Audio;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.Font;
using Stride.Input;
using Stride.Physics;
using Stride.Profiling;
using Stride.Rendering;
using Stride.Rendering.Fonts;
using Stride.Rendering.Sprites;
using Stride.Shaders.Compiler;
using Stride.UI;
using System;

namespace MultiplayerExample.Engine
{
    class GameEngineClient : GameEngineBase
    {
        private readonly IServiceRegistry _globalServices;  // HACK: required for loading graphics assets

        private readonly GameSystemKeyValue<NetworkSystem> _networkSystem;
        private readonly GameSystemKeyValue<DebugTextSystem> _debugTextSystem;
        private readonly GameSystemKeyValue<Bullet2PhysicsSystem> _physicsSystem;
        private readonly GameSystemKeyValue<ScriptSystem> _scriptSystem;
        private readonly GameSystemKeyValue<ScenePreUpdateSystem> _scenePreUpdateSystem;
        private readonly GameSystemKeyValue<SceneSystem> _sceneSystem;
        private readonly GameSystemKeyValue<ScenePostUpdateSystem> _scenePostUpdateSystem;
        private readonly GameSystemKeyValue<AudioSystem> _audioSystem;
        //private readonly GameSystemKeyValue<GameFontSystem> _gameFontSystem;
        private readonly GameSystemKeyValue<EffectSystem> _effectSystem;
        private readonly GameSystemKeyValue<SpriteAnimationSystem> _spriteAnimationSystem;
        private readonly GameSystemKeyValue<UISystem> _uiSystem;
        private readonly GameSystemKeyValue<GameProfilingSystem> _profilingSystem;
        //private readonly GameSystemKeyValue<DynamicNavigationMeshSystem> _dynamicNavigationMeshSystem;

        private GraphicsContext _graphicsContext;           // HACK: required for loading graphics assets

        public readonly GameTimeExt RenderTime = new GameTimeExt();

        public GameEngineClient(ContentManager contentManager, IServiceRegistry globalServices)
            : base(contentManager, globalServices)
        {
            _globalServices = globalServices;

            Services.AddService(globalServices.GetSafeServiceAs<IGame>());      // Set first because Stride specific systems depends on IGame's existence. Must be careful SERVER side systems cannot use this!
            Services.AddService(new GameEngineContext(isServer: false));

            _networkSystem = CreateKeyValue(new NetworkSystem(Services));

            _debugTextSystem = CreateKeyValue(new DebugTextSystem(Services));
            Services.AddService(_debugTextSystem.System);

            _physicsSystem = CreateKeyValue(new Bullet2PhysicsSystem(Services));
            _sceneSystem = CreateKeyValue(new SceneSystem(Services));
            _scenePreUpdateSystem = CreateKeyValue(new ScenePreUpdateSystem(Services, _sceneSystem.System));
            _scenePostUpdateSystem = CreateKeyValue(new ScenePostUpdateSystem(Services, _sceneSystem.System));

            _scriptSystem = CreateKeyValue(new ScriptSystem(Services));
            Services.AddService(_scriptSystem.System);

            _audioSystem = CreateKeyValue(new AudioSystem(Services));
            Services.AddService(_audioSystem.System);
            Services.AddService<IAudioEngineProvider>(_audioSystem.System);

            _effectSystem = CreateKeyValue(new EffectSystem(Services));
            Services.AddService(_effectSystem.System);

            //_gameFontSystem = CreateKeyValue(new GameFontSystem(Services));
            var gameFontSystem = globalServices.GetSafeServiceAs<GameFontSystem>();
            Services.AddService(gameFontSystem.FontSystem);
            Services.AddService<IFontFactory>(gameFontSystem.FontSystem);

            _spriteAnimationSystem = CreateKeyValue(new SpriteAnimationSystem(Services));
            Services.AddService(_spriteAnimationSystem.System);

            _uiSystem = CreateKeyValue(new UISystem(Services));
            Services.AddService(_uiSystem.System);

            _profilingSystem = CreateKeyValue(new GameProfilingSystem(Services));
            Services.AddService(_profilingSystem.System);

            //VRDeviceSystem = new VRDeviceSystem(Services);
            //Services.AddService(VRDeviceSystem);

            // Copy access to the graphics device manager
            Services.AddService(globalServices.GetSafeServiceAs<IGraphicsDeviceManager>());
            Services.AddService(globalServices.GetSafeServiceAs<IGraphicsDeviceService>());

            //_dynamicNavigationMeshSystem = CreateKeyValue(new DynamicNavigationMeshSystem(Services));
        }

        protected override void OnInitialize(IServiceRegistry globalServices)
        {
            var gameSettingsService = Services.GetService<IGameSettingsService>();
            var gameSettings = gameSettingsService.Settings;
            var renderingSettings = gameSettings?.Configurations?.Get<RenderingSettings>() ?? new RenderingSettings();
            // Load several default settings
            //if (AutoLoadDefaultSettings)
            {
                var deviceManager = (GraphicsDeviceManager)Services.GetSafeServiceAs<IGraphicsDeviceManager>();
                if (renderingSettings.DefaultGraphicsProfile > 0)
                {
                    deviceManager.PreferredGraphicsProfile = new[] { renderingSettings.DefaultGraphicsProfile };
                }

                if (renderingSettings.DefaultBackBufferWidth > 0)
                {
                    deviceManager.PreferredBackBufferWidth = renderingSettings.DefaultBackBufferWidth;
                }
                if (renderingSettings.DefaultBackBufferHeight > 0)
                {
                    deviceManager.PreferredBackBufferHeight = renderingSettings.DefaultBackBufferHeight;
                }

                deviceManager.PreferredColorSpace = renderingSettings.ColorSpace;
                _sceneSystem.System.InitialSceneUrl = Settings?.DefaultSceneUrl;
                _sceneSystem.System.InitialGraphicsCompositorUrl = Settings?.DefaultGraphicsCompositorUrl;
                _sceneSystem.System.SplashScreenUrl = Settings?.SplashScreenUrl;
                _sceneSystem.System.SplashScreenColor = Settings?.SplashScreenColor ?? Color4.Black;
                _sceneSystem.System.DoubleViewSplashScreen = Settings?.DoubleViewSplashScreen ?? false;
            }

            // ---------------------------------------------------------
            // Add common GameSystems - Adding order is important
            // (Unless overriden by gameSystem.UpdateOrder)
            // ---------------------------------------------------------

            // Add the input manager
            // Add it first so that it can obtained by the UI system
            var inputManager = globalServices.GetSafeServiceAs<InputManager>();
            //Input = new InputManager(Services);
            Services.AddService(inputManager);
            //GameSystems.Add(inputManager);    // Updated by the Game class

            // Initialize the systems
            GameSystems.Initialize();

            Services.AddService<IGameSystemCollection>(GameSystems);

            GameSystems.Add(_debugTextSystem.System);

            // Physics occurs before scripts
            Services.AddService<IPhysicsSystem>(_physicsSystem.System);
            GameSystems.Add(_physicsSystem.System);

            // Add the scheduler system
            // - Must be after Input, so that scripts are able to get latest input
            // - Must be before Entities/Camera/Audio/UI, so that scripts can apply
            // changes in the same frame they will be applied
            GameSystems.Add(_scriptSystem.System);

            Services.AddService(_scenePreUpdateSystem.System);
            GameSystems.Add(_scenePreUpdateSystem.System);

            Services.AddService(_sceneSystem.System);
            GameSystems.Add(_sceneSystem.System);

            Services.AddService(_scenePostUpdateSystem.System);
            GameSystems.Add(_scenePostUpdateSystem.System);

            GameSystems.Add(_audioSystem.System);
            //GameSystems.Add(_gameFontSystem.System);

            // If requested in game settings, compile effects remotely and/or notify new shader requests
            var effectSystem = _effectSystem.System;
            effectSystem.Compiler = EffectCompilerFactory.CreateEffectCompiler(Content.FileProvider, effectSystem, Settings?.PackageName, Settings?.EffectCompilation ?? EffectCompilationMode.Local, Settings?.RecordUsedEffects ?? false);
            // Setup shader compiler settings from a compilation mode.
            // TODO: We might want to provide overrides on the GameSettings to specify debug and/or optim level specifically.
            if (Settings != null)
            {
                effectSystem.SetCompilationMode(Settings.CompilationMode);
            }
            GameSystems.Add(_effectSystem.System);

            GameSystems.Add(_spriteAnimationSystem.System);
            GameSystems.Add(_uiSystem.System);
            GameSystems.Add(_profilingSystem.System);
            //var dynamicNavigationMeshSystem = new Stride.Navigation.DynamicNavigationMeshSystem(_services);
            //GameSystems.Add(dynamicNavigationMeshSystem);

            GameSystems.Add(_networkSystem.System);     // Make sure this is added AFTER _sceneSystem due to dependency on it
        }

        protected override void OnLoadContent()
        {
            _sceneSystem.System.SceneInstance.RootSceneChanged += OnRootSceneChanged;
            OnRootSceneChanged(this, EventArgs.Empty);
        }

        private void OnRootSceneChanged(object sender, EventArgs e)
        {
            var rootScene = _sceneSystem.System.SceneInstance.RootScene;
            if (rootScene == null)
            {
                return;
            }
            var sceneManager = _sceneSystem.System.GetSceneManagerFromRootScene();
            var clientDataScene = sceneManager.LoadSceneSync(sceneManager.RootClientOnlyDataSceneUrl);
            clientDataScene.MergeSceneTo(rootScene);
        }

        public override void InitialUpdate()
        {
            UpdateGameSystems(UpdateTime, updatePhysicsSimulation: true, PhysicsGameTime);
        }

        protected override void UpdateGameSystems(GameTimeExt gameTime, bool updatePhysicsSimulation, GameTimeExt physicsGameTime)
        {
            // While GameSystems.Update(gameTime) can be used, I prefer seeing explicitly what I'm updating
            // (easier to place breakpoints, as well)
            _debugTextSystem.TryUpdate(gameTime);

            _networkSystem.TryUpdate(gameTime);

            _scenePreUpdateSystem.TryUpdate(gameTime);  // Must occur before the physics & scripts

            if (updatePhysicsSimulation)
            {
                _physicsSystem.TryUpdate(physicsGameTime);
#if DEBUG
                //System.Diagnostics.Debug.WriteLine($"GameTime: {gameTime.Total.TotalMilliseconds} - PhysTime: {physicsGameTime.Total.TotalMilliseconds}");
#endif
            }

            // Scripts are run before everything (except physics)
            _scriptSystem.TryUpdate(gameTime);

            _audioSystem.TryUpdate(gameTime);
            //_gameFontSystem.TryUpdate(gameTime);
            _effectSystem.TryUpdate(gameTime);
            _spriteAnimationSystem.TryUpdate(gameTime);
            _uiSystem.TryUpdate(gameTime);

            //_dynamicNavigationMeshSystem.TryUpdate(gameTime);

            _sceneSystem.TryUpdate(gameTime);   // This runs all the standard entity processors
            _scenePostUpdateSystem.TryUpdate(gameTime);

            _profilingSystem.TryUpdate(gameTime);
        }

        protected override void UpdateDrawTimer(GameTime updateTime)
        {
            var elapsedTime = updateTime.Total - RenderTime.Total;
            RenderTime.Update(RenderTime.Total + elapsedTime, elapsedTime, incrementFrameCount: true);
        }

        public sealed override bool BeginDraw()
        {
            if (_graphicsContext == null)
            {
                _graphicsContext = _globalServices.GetSafeServiceAs<GraphicsContext>();
                Services.AddService(_graphicsContext);
            }
            return true;
        }

        public sealed override void Draw()
        {
            // The draw order can be left to whatever the systems's current orders are.
            GameSystems.Draw(RenderTime);
        }

        //public override void EndDraw() { }
    }
}
