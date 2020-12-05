// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using MultiplayerExample.Engine;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Data;
using Stride.Engine.Design;
using Stride.Games;
using Stride.Games.Time;
using Stride.Physics;
using System;
using System.Reflection;

namespace MultiplayerExample
{
    /* This is a slimmed down version of Stride's Game/GameBase classes.
     * Note that this CANNOT inherit Stride's Game class, because it tries to set up GraphicsDevice, Audio, etc.
     * However, this also means we must be careful to NOT touch any classes that try to use GraphicsDevice, Audio, etc.
     * This means our assets cannot use SyncScripts/AsyncScripts since they always try to get GraphicsDevice, Audio, etc.
     */
    public class GameAppServer : IDisposable, IGameSettingsService, IExitGameService
    {
        private readonly ILogger _logger;

        private readonly ServiceRegistry _services = new ServiceRegistry();

        private GameEngineServer _gameEngine;

        private readonly TimerTick _autoTickTimer = new TimerTick();
        private readonly GameTimeExt _updateTime = new GameTimeExt();

        private bool _initializeDatabase = true;

        private DatabaseFileProvider _databaseFileProvider;

        public ContentManager Content { get; private set; }

        public GameSettings Settings { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is exiting.
        /// </summary>
        /// <value><c>true</c> if this instance is exiting; otherwise, <c>false</c>.</value>
        public bool IsExiting { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        public bool IsRunning { get; private set; }

        public GameAppServer()
        {
            _logger = GlobalLogger.GetLogger(GetType().GetTypeInfo().Name);

            // Database file provider
            _services.AddService<IDatabaseFileProviderService>(new DatabaseFileProviderService(null));

            _services.AddService(GameEngineServer.DefaultGraphicsDeviceService);  // ContentManager requires a GraphicsDeviceService if graphical assets are loaded (eg. Textures)
            _services.AddService<IExitGameService>(this);
        }

        /// <summary>
        /// Exits the game.
        /// </summary>
        public void Exit()
        {
            IsExiting = true;
        }

        /// <summary>
        /// Call this method to initialize the game, begin running the game loop, and start processing events for the game.
        /// </summary>
        /// <exception cref="InvalidOperationException">Cannot run this instance while it is already running</exception>
        public void Run()
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("Cannot run this instance while it is already running");
            }

            PrepareContext();

            InitializeBeforeRun();

            try
            {
                while (true)
                {
                    //lock (_tickLock)
                    {
                        // If this instance is existing, then don't make any further update calls
                        if (IsExiting)
                        {
                            CheckEndRun();
                            break;
                        }

                        DoTick();
                    }
                }
            }
            finally
            {
            }
        }

        private void DoTick()
        {
            try
            {
                // Update the timer
                _autoTickTimer.Tick();

                var elapsedTime = _autoTickTimer.ElapsedTimeWithPause;
                using (Profiler.Begin(GameProfilingKeys.GameUpdate))
                {
                    _updateTime.Update(_updateTime.Total + elapsedTime, elapsedTime, incrementFrameCount: true);
                    _gameEngine.Update();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected exception.", ex);
                throw;
            }
            finally
            {
                CheckEndRun();
            }
        }

        private void CheckEndRun()
        {
            if (IsExiting && IsRunning && _gameEngine.IsFullyShutDown)
            {
                EndRun();
                IsRunning = false;
            }
        }

        private void PrepareContext()
        {
            // Content manager (this will be shared to all the EngineCores)
            Content = new ContentManager(_services);
            _services.AddService<IContentManager>(Content);
            _services.AddService(Content);

            // Initialize assets
            if (_initializeDatabase)
            {
                _databaseFileProvider = InitializeAssetDatabase();
                ((DatabaseFileProviderService)_services.GetService<IDatabaseFileProviderService>()).FileProvider = _databaseFileProvider;

                if (Content.Exists(GameSettings.AssetUrl))  // TODO: maybe server needs its own GameSettings asset url?
                {
                    Settings = Content.Load<GameSettings>(GameSettings.AssetUrl);
                }
                else
                {
                    Settings = new GameSettings
                    {
                        Configurations = new PlatformConfigurations(),
                    };
                    //var navSettings = Settings.Configurations.Get<NavigationSettings>();
                    //if (navSettings == null)
                    //{
                    //    var navConfigSettings = new ConfigurationOverride
                    //    {
                    //        Configuration = navSettings
                    //    };
                    //    Settings.Configurations.Configurations.Add(navConfigSettings);
                    //}
                }
                _services.AddService<IGameSettingsService>(this);
            }
            // HACK (kind of): Server must run at a fixed rate, which we'll manually control with _physicGameTime
            var physicsSettings = Settings.Configurations.Get<PhysicsSettings>() ?? new PhysicsSettings();
            physicsSettings.Flags = PhysicsEngineFlags.ContinuousCollisionDetection;
            physicsSettings.MaxSubSteps = 0;    // Important to keep this at 0 since this makes BulletPhysics simulate exactly one step per update
            physicsSettings.FixedTimeStep = 1f / GameConfig.PhysicsSimulationRate;
            var physicsConfigSettings = new ConfigurationOverride
            {
                Configuration = physicsSettings
            };
            Settings.Configurations.Configurations.Add(physicsConfigSettings);
        }

        private void InitializeBeforeRun()
        {
            try
            {
                using (var profile = Profiler.Begin(GameProfilingKeys.GameInitialize))
                {
                    // Initialize this instance and all game systems before trying to create the device.
                    Initialize();

                    _gameEngine.LoadContent();

                    IsRunning = true;

                    BeginRun();

                    _autoTickTimer.Reset();
                    _updateTime.Reset(_updateTime.Total);

                    // Run the first time an update
                    using (Profiler.Begin(GameProfilingKeys.GameUpdate))
                    {
                        _gameEngine.InitialUpdate();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected exception.", ex);
                throw;
            }
        }

        private void Initialize()
        {
            Content.Serializer.LowLevelSerializerSelector = ParameterContainerExtensions.DefaultSceneSerializerSelector;

            _gameEngine = new GameEngineServer(Content, _services);
            _gameEngine.Initialize();
        }

        /// <summary>
        /// Called after all components are initialized, before the game loop starts.
        /// </summary>
        private void BeginRun()
        {
        }

        /// <summary>
        /// Called after the game loop has stopped running before exiting.
        /// </summary>
        private void EndRun()
        {
        }

        internal static DatabaseFileProvider InitializeAssetDatabase()
        {
            using (Profiler.Begin(GameProfilingKeys.ObjectDatabaseInitialize))
            {
                // Create and mount database file system
                var objDatabase = ObjectDatabase.CreateDefaultDatabase();

                // Only set a mount path if not mounted already
                var mountPath = VirtualFileSystem.ResolveProviderUnsafe("/asset", true).Provider == null ? "/asset" : null;
                var result = new DatabaseFileProvider(objDatabase, mountPath);

                return result;
            }
        }

        void IDisposable.Dispose()
        {
            //throw new NotImplementedException();
        }
    }
}
