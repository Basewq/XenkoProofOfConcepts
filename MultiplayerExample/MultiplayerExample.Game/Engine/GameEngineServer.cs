using MultiplayerExample.Network;
using Stride.Core;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Physics;

namespace MultiplayerExample.Engine
{
    /// <summary>
    /// The main 'game' class with all the graphics related systems/services are removed (or dummied out where it can't be removed)
    /// </summary>
    class GameEngineServer : GameEngineBase
    {
        public static IGraphicsDeviceService DefaultGraphicsDeviceService { get; } = new HeadlessGraphicsDeviceService();

        private readonly GameSystemKeyValue<NetworkSystem> _networkSystem;
        //private readonly GameSystemKeyValue<ScriptSystem> _scriptSystem;
        private readonly GameSystemKeyValue<Bullet2PhysicsSystem> _physicsSystem;
        private readonly GameSystemKeyValue<ScenePreUpdateSystem> _scenePreUpdateSystem;
        private readonly GameSystemKeyValue<SceneSystem> _sceneSystem;
        private readonly GameSystemKeyValue<ScenePostUpdateSystem> _scenePostUpdateSystem;
        //private readonly GameSystemKeyValue<DynamicNavigationMeshSystem> _dynamicNavigationMeshSystem;

        private IGameNetworkService _networkService;

        public GameEngineServer(ContentManager contentManager, IServiceRegistry globalServices)
            : base(contentManager, globalServices)
        {
            // TODO: load content manager stuff accounting for ContentManagerLoaderSettings

            Services.AddService(new GameEngineContext(isServer: true));

            _networkSystem = CreateKeyValue(new NetworkSystem(Services));
            //_scriptSystem = CreateKeyValue(new ScriptSystem(Services));
            _physicsSystem = CreateKeyValue(new Bullet2PhysicsSystem(Services));
            _sceneSystem = CreateKeyValue(new HeadlessSceneSystem(Services) as SceneSystem);
            _scenePreUpdateSystem = CreateKeyValue(new ScenePreUpdateSystem(Services, _sceneSystem.System));
            _scenePostUpdateSystem = CreateKeyValue(new ScenePostUpdateSystem(Services, _sceneSystem.System));
            //_dynamicNavigationMeshSystem = CreateKeyValue(new DynamicNavigationMeshSystem(Services));

            Services.AddService(DefaultGraphicsDeviceService);

            _networkService = _networkSystem.System;
        }

        protected override void OnInitialize(IServiceRegistry globalServices)
        {
            _sceneSystem.System.InitialSceneUrl = Settings?.DefaultSceneUrl;

            // ---------------------------------------------------------
            // Add common GameSystems - Adding order is important
            // (Unless overriden by gameSystem.UpdateOrder)
            // ---------------------------------------------------------

            // Add the input manager
            // Add it first so that it can obtained by the UI system
            //Input = new InputManager(Services);
            //Services.AddService(Input);
            //GameSystems.Add(Input);

            // Initialize the systems
            GameSystems.Initialize();

            Services.AddService<IGameSystemCollection>(GameSystems);

            // Add the scheduler system
            // - Must be after Input, so that scripts are able to get latest input
            // - Must be before Entities/Camera/Audio/UI, so that scripts can apply
            // changes in the same frame they will be applied
            //GameSystems.Add(_scriptSystem.System);

            // Add the Font system
            //GameSystems.Add(gameFontSystem);

            // Add the Audio System
            //GameSystems.Add(Audio);

            //var dynamicNavigationMeshSystem = new Stride.Navigation.DynamicNavigationMeshSystem(_services);
            //GameSystems.Add(dynamicNavigationMeshSystem);

            Services.AddService(_scenePreUpdateSystem.System);
            GameSystems.Add(_scenePreUpdateSystem.System);

            Services.AddService(_sceneSystem.System);
            GameSystems.Add(_sceneSystem.System);

            Services.AddService(_scenePostUpdateSystem.System);
            GameSystems.Add(_scenePostUpdateSystem.System);

            Services.AddService<IPhysicsSystem>(_physicsSystem.System);
            GameSystems.Add(_physicsSystem.System);

            GameSystems.Add(_networkSystem.System);     // Make sure this is added AFTER _sceneSystem due to dependency on it
        }

        public override void InitialUpdate()
        {
            _networkService.StartDedicatedServer();
            UpdateGameSystems(UpdateTime, updatePhysicsSimulation: true, PhysicsGameTime);
        }

        protected override void UpdateGameSystems(GameTimeExt gameTime, bool updatePhysicsSimulation, GameTimeExt physicsGameTime)
        {
            _networkSystem.TryUpdate(gameTime);

            _scenePreUpdateSystem.TryUpdate(gameTime);

            if (updatePhysicsSimulation)
            {
                _physicsSystem.TryUpdate(physicsGameTime);
#if DEBUG
                //System.Diagnostics.Debug.WriteLine($"GameTime: {gameTime.Total.TotalMilliseconds} - PhysTime: {physicsGameTime.Total.TotalMilliseconds}");
#endif
            }

#if DEBUG
            //System.Diagnostics.Debug.WriteLine(@$"Time: {GameClockManager.SimulationClock.TotalTime:hh\:mm\:ss\.ff} - TickNo: {GameClockManager.SimulationClock.SimulationTickNumber}");
#endif
            //_scriptSystem.TryUpdate(gameTime);
            _sceneSystem.TryUpdate(gameTime);   // This runs all the standard entity processors
            _scenePostUpdateSystem.TryUpdate(gameTime);
            //_dynamicNavigationMeshSystem.TryUpdate(gameTime);
        }
    }
}
