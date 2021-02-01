using MultiplayerExample.Core;
using MultiplayerExample.GameServices;
using MultiplayerExample.Network;
using MultiplayerExample.Utilities;
using Stride.Audio;
using Stride.Core;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
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
using Stride.Streaming;
using Stride.UI;
using System;
using System.Linq;

namespace MultiplayerExample.Engine
{
    class GameEngineClient : GameEngineBase
    {
        private readonly GameSystemKeyValue<NetworkSystem> _networkSystem;
        private readonly GameSystemKeyValue<DebugTextSystem> _debugTextSystem;
        private readonly GameSystemKeyValue<Bullet2PhysicsSystem> _physicsSystem;
        private readonly GameSystemKeyValue<ScriptSystem> _scriptSystem;
        private readonly GameSystemKeyValue<ScenePreUpdateSystem> _scenePreUpdateSystem;
        private readonly GameSystemKeyValue<SceneSystem> _sceneSystem;
        private readonly GameSystemKeyValue<ScenePostUpdateSystem> _scenePostUpdateSystem;

        private readonly GameSystemKeyValue<InputSystem> _inputSystem;
        private readonly GameSystemKeyValue<StreamingManager> _streamingManager;
        private readonly GameSystemKeyValue<AudioSystem> _audioSystem;
        private readonly GameSystemKeyValue<GameFontSystem> _gameFontSystem;
        private readonly GameSystemKeyValue<EffectSystem> _effectSystem;
        private readonly GameSystemKeyValue<SpriteAnimationSystem> _spriteAnimationSystem;
        private readonly GameSystemKeyValue<UISystem> _uiSystem;
        private readonly GameSystemKeyValue<GameProfilingSystem> _profilingSystem;
        //private readonly GameSystemKeyValue<DynamicNavigationMeshSystem> _dynamicNavigationMeshSystem;
        //private readonly GameSystemKeyValue<VRDeviceSystem> _vrDeviceSystem;

        public readonly GameTimeExt RenderTime = new GameTimeExt();

        public GameEngineClient(ContentManager contentManager, IServiceRegistry services, GameSystemCollection gameSystems)
            : base(contentManager, services, gameSystems)
        {
            // Note that IGame already part of Services in the client engine because Stride specific systems depends
            // on IGame's existence, however be aware this is NOT included in GameEngineServer, so you must be careful
            // not to get IGame in server side systems.
            Services.AddOrOverwriteService(new GameEngineContext(isClient: true));

            _networkSystem = CreateKeyValue(() => new NetworkSystem(Services));

            _debugTextSystem = CreateKeyValue(() => new DebugTextSystem(Services));
            Services.AddOrOverwriteService(_debugTextSystem.System);

            _physicsSystem = CreateKeyValue(() => new Bullet2PhysicsSystem(Services));
            _sceneSystem = CreateKeyValue(() => new SceneSystem(Services));
            _scenePreUpdateSystem = CreateKeyValue(() => new ScenePreUpdateSystem(Services, _sceneSystem.System));
            _scenePostUpdateSystem = CreateKeyValue(() => new ScenePostUpdateSystem(Services, _sceneSystem.System));

            _scriptSystem = CreateKeyValue(() => new ScriptSystem(Services));
            Services.AddOrOverwriteService(_scriptSystem.System);

            var inputSystem = gameSystems.First(x => x is InputSystem) as InputSystem;
            _inputSystem = CreateKeyValue(inputSystem);
            var inputManager = inputSystem.Manager;
            inputManager.VirtualButtonConfigSet = new VirtualButtonConfigSet();

            _streamingManager = CreateKeyValue(() => new StreamingManager(Services));

            _audioSystem = CreateKeyValue(() => new AudioSystem(Services));
            Services.AddOrOverwriteService(_audioSystem.System);
            Services.AddOrOverwriteService<IAudioEngineProvider>(_audioSystem.System);

            _effectSystem = CreateKeyValue(() => new EffectSystem(Services));
            Services.AddOrOverwriteService(_effectSystem.System);

            _gameFontSystem = CreateKeyValue(() => new GameFontSystem(Services));
            Services.AddOrOverwriteService(_gameFontSystem.System.FontSystem);
            Services.AddOrOverwriteService<IFontFactory>(_gameFontSystem.System.FontSystem);

            _spriteAnimationSystem = CreateKeyValue(() => new SpriteAnimationSystem(Services));
            Services.AddOrOverwriteService(_spriteAnimationSystem.System);

            _uiSystem = CreateKeyValue(() => new UISystem(Services));
            Services.AddOrOverwriteService(_uiSystem.System);

            _profilingSystem = CreateKeyValue(() => new GameProfilingSystem(Services));
            Services.AddOrOverwriteService(_profilingSystem.System);

            //_vrDeviceSystem = CreateKeyValue(() => new VRDeviceSystem(Services));
            //Services.AddOrOverwriteService(_vrDeviceSystem.System);

            // Copy access to the graphics device manager
            Services.AddOrOverwriteService(services.GetSafeServiceAs<IGraphicsDeviceManager>());
            Services.AddOrOverwriteService(services.GetSafeServiceAs<IGraphicsDeviceService>());

            //_dynamicNavigationMeshSystem = CreateKeyValue(() => new DynamicNavigationMeshSystem(Services));
        }

        protected override void OnInitialize()
        {
            // Initialize the systems
            //GameSystems.Initialize();     // Already initialized by Game class

            GameSystems.TryAdd(_debugTextSystem.System);

            Services.AddOrOverwriteService<IPhysicsSystem>(_physicsSystem.System);
            GameSystems.TryAdd(_physicsSystem.System);

            GameSystems.TryAdd(_scriptSystem.System);

            Services.AddOrOverwriteService(_scenePreUpdateSystem.System);
            GameSystems.TryAdd(_scenePreUpdateSystem.System);

            Services.AddOrOverwriteService(_sceneSystem.System);
            GameSystems.TryAdd(_sceneSystem.System);

            Services.AddOrOverwriteService(_scenePostUpdateSystem.System);
            GameSystems.TryAdd(_scenePostUpdateSystem.System);

            GameSystems.TryAdd(_audioSystem.System);
            GameSystems.TryAdd(_gameFontSystem.System);

            GameSystems.TryAdd(_effectSystem.System);

            GameSystems.TryAdd(_spriteAnimationSystem.System);
            GameSystems.TryAdd(_uiSystem.System);
            GameSystems.TryAdd(_profilingSystem.System);
            //var dynamicNavigationMeshSystem = new Stride.Navigation.DynamicNavigationMeshSystem(_services);
            //GameSystems.TryAdd(dynamicNavigationMeshSystem);
            //GameSystems.TryAdd(_vrDeviceSystem);

            GameSystems.TryAdd(_networkSystem.System);     // Make sure this is added AFTER _sceneSystem due to dependency on it
        }

        protected override void OnLoadContent()
        {
            //GameSystems.LoadContent();    // Already done in Game class
            //((IContentable)_gameFontSystem.System).LoadContent();
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
            GameSystemsUpdate(updatePhysicsSimulation: true, updateSingleCallSystems: true);
        }

        protected override void GameSystemsUpdate(bool updatePhysicsSimulation, bool updateSingleCallSystems)
        {
            // Don't use GameSystems.Update(updateTime), because we use different GameTimes depending on
            // what we're trying to update.
            _debugTextSystem.UpdateIfEnabled(SingleCallSystemsGameTime, updateSingleCallSystems);
            //_vrDeviceSystem.TryUpdate(SingleCallSystemsGameTime, updateSingleCallSystems);
            _inputSystem.UpdateIfEnabled(SingleCallSystemsGameTime, updateSingleCallSystems);

            _networkSystem.UpdateIfEnabled(UpdateTime);

            _scenePreUpdateSystem.UpdateIfEnabled(UpdateTime);  // Must occur before the physics & scripts

            if (updatePhysicsSimulation)
            {
                _physicsSystem.UpdateIfEnabled(PhysicsGameTime);
#if DEBUG
                //System.Diagnostics.Debug.WriteLine($"GameTime: {UpdateTime.Total.TotalMilliseconds} - PhysTime: {physicsGameTime.Total.TotalMilliseconds}");
#endif
            }
            // Scripts are run before everything (except physics)
            _scriptSystem.UpdateIfEnabled(UpdateTime);

            _streamingManager.UpdateIfEnabled(SingleCallSystemsGameTime, updateSingleCallSystems);
            _audioSystem.UpdateIfEnabled(SingleCallSystemsGameTime, updateSingleCallSystems);
            _gameFontSystem.UpdateIfEnabled(SingleCallSystemsGameTime, updateSingleCallSystems);
            _effectSystem.UpdateIfEnabled(SingleCallSystemsGameTime, updateSingleCallSystems);
            _spriteAnimationSystem.UpdateIfEnabled(SingleCallSystemsGameTime, updateSingleCallSystems);
            _uiSystem.UpdateIfEnabled(SingleCallSystemsGameTime, updateSingleCallSystems);

            //_dynamicNavigationMeshSystem.TryUpdate(SingleCallSystemsGameTime, updateSingleCallSystems);

            _sceneSystem.UpdateIfEnabled(UpdateTime);   // This runs all the standard entity processors
            _scenePostUpdateSystem.UpdateIfEnabled(UpdateTime);

            _profilingSystem.UpdateIfEnabled(SingleCallSystemsGameTime, updateSingleCallSystems);
        }

        protected override void GameSystemsPostUpdate()
        {
            // Update the render time
            var elapsedTime = UpdateTime.Total - RenderTime.Total;
            RenderTime.Update(UpdateTime.Total, elapsedTime, incrementFrameCount: true);
        }

        public sealed override bool BeginDraw() => true;

        public sealed override void Draw()
        {
            // The draw order can be left to whatever the systems's current orders are.
            GameSystems.Draw(RenderTime);
        }

        //public override void EndDraw() { }
    }
}
