using MultiplayerExample.Network;
using MultiplayerExample.Utilities;
using Stride.Core;
using Stride.Core.Serialization.Contents;
using Stride.Core.Streaming;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.Data;
using Stride.Physics;
using Stride.Streaming;

namespace MultiplayerExample.Engine
{
    /// <summary>
    /// The main 'game' class with all the graphics related systems/services are removed (or dummied out where it can't be removed)
    /// </summary>
    class GameEngineServer : GameEngineBase
    {
        public const ushort DefaultServerPortNumber = 60000;   // TODO: should probably be a config setting

        public static IGraphicsDeviceService DefaultGraphicsDeviceService { get; } = new HeadlessGraphicsDeviceService();

        private readonly GameSystemKeyValue<NetworkSystem> _networkSystem;
        //private readonly GameSystemKeyValue<ScriptSystem> _scriptSystem;
        private readonly GameSystemKeyValue<Bullet2PhysicsSystem> _physicsSystem;
        private readonly GameSystemKeyValue<ScenePreUpdateSystem> _scenePreUpdateSystem;
        private readonly GameSystemKeyValue<SceneSystem> _sceneSystem;
        private readonly GameSystemKeyValue<ScenePostUpdateSystem> _scenePostUpdateSystem;
        private readonly GameSystemKeyValue<StreamingManager> _streamingManager;
        //private readonly GameSystemKeyValue<DynamicNavigationMeshSystem> _dynamicNavigationMeshSystem;

        private IGameNetworkService _networkService;

        public GameEngineServer(ContentManager contentManager, IServiceRegistry services)
            : base(contentManager, services)
        {
            // Note that IGame already part of Services in the client engine because Stride specific systems depends
            // on IGame's existence, however be aware this is NOT included in GameEngineServer, so you must be careful
            // not to get IGame in server side systems.

            Services.AddOrOverwriteService(new GameEngineContext(isClient: false));

            _networkSystem = CreateKeyValue(new NetworkSystem(Services));
            //_scriptSystem = CreateKeyValue(new ScriptSystem(Services));
            _physicsSystem = CreateKeyValue(new Bullet2PhysicsSystem(Services));
            _sceneSystem = CreateKeyValue(new HeadlessSceneSystem(Services) as SceneSystem);
            _scenePreUpdateSystem = CreateKeyValue(new ScenePreUpdateSystem(Services, _sceneSystem.System));
            _scenePostUpdateSystem = CreateKeyValue(new ScenePostUpdateSystem(Services, _sceneSystem.System));
            _streamingManager = CreateKeyValue(() => new StreamingManager(Services));
            //_dynamicNavigationMeshSystem = CreateKeyValue(new DynamicNavigationMeshSystem(Services));

            Services.AddOrOverwriteService(_streamingManager.System);
            Services.AddOrOverwriteService<IStreamingManager>(_streamingManager.System);
            Services.AddOrOverwriteService<ITexturesStreamingProvider>(_streamingManager.System);

            Services.AddOrOverwriteService(DefaultGraphicsDeviceService);

            _networkService = _networkSystem.System;
        }

        protected override void OnInitialize()
        {
            if (Settings != null)
            {
                _streamingManager.System.SetStreamingSettings(Settings.Configurations.Get<StreamingSettings>());
            }
            _sceneSystem.System.InitialSceneUrl = Settings?.DefaultSceneUrl;

            // Add the input manager
            // Add it first so that it can obtained by the UI system
            //Input = new InputManager(Services);
            //Services.AddOrOverwriteService(Input);
            //GameSystems.Add(Input);

            // Initialize the systems
            GameSystems.Initialize();

            Services.AddOrOverwriteService<IGameSystemCollection>(GameSystems);

            //GameSystems.Add(_scriptSystem.System);
            //GameSystems.Add(gameFontSystem);
            //GameSystems.Add(Audio);

            //var dynamicNavigationMeshSystem = new Stride.Navigation.DynamicNavigationMeshSystem(_services);
            //GameSystems.Add(dynamicNavigationMeshSystem);

            Services.AddOrOverwriteService(_scenePreUpdateSystem.System);
            GameSystems.Add(_scenePreUpdateSystem.System);

            Services.AddOrOverwriteService(_sceneSystem.System);
            GameSystems.Add(_sceneSystem.System);

            Services.AddOrOverwriteService(_scenePostUpdateSystem.System);
            GameSystems.Add(_scenePostUpdateSystem.System);

            Services.AddOrOverwriteService<IPhysicsSystem>(_physicsSystem.System);
            GameSystems.Add(_physicsSystem.System);

            GameSystems.Add(_networkSystem.System);     // Make sure this is added AFTER _sceneSystem due to dependency on it
        }

        protected override void OnLoadContent()
        {
            GameSystems.LoadContent();
        }

        public override void InitialUpdate()
        {
            _networkService.StartDedicatedServer(DefaultServerPortNumber);
            GameSystemsUpdate(updatePhysicsSimulation: true, updateSingleCallSystems: true);
        }

        protected override void GameSystemsUpdate(bool updatePhysicsSimulation, bool updateSingleCallSystems)
        {
            _networkSystem.UpdateIfEnabled(SingleCallSystemsGameTime, updateSingleCallSystems);

            _scenePreUpdateSystem.UpdateIfEnabled(UpdateTime);

            if (updatePhysicsSimulation)
            {
                _physicsSystem.UpdateIfEnabled(PhysicsGameTime);
#if DEBUG
                //System.Diagnostics.Debug.WriteLine($"GameTime: {UpdateTime.Total.TotalMilliseconds} - PhysTime: {physicsGameTime.Total.TotalMilliseconds}");
#endif
            }

#if DEBUG
            //System.Diagnostics.Debug.WriteLine(@$"Time: {GameClockManager.SimulationClock.TotalTime:hh\:mm\:ss\.ff} - TickNo: {GameClockManager.SimulationClock.SimulationTickNumber}");
#endif
            //_scriptSystem.TryUpdate(UpdateTime);
            _streamingManager.UpdateIfEnabled(SingleCallSystemsGameTime, updateSingleCallSystems);

            _sceneSystem.UpdateIfEnabled(UpdateTime);   // This runs all the standard entity processors
            _scenePostUpdateSystem.UpdateIfEnabled(UpdateTime);
            //_dynamicNavigationMeshSystem.TryUpdate(SingleCallSystemsGameTime, updateSingleCallSystems);
        }
    }
}
