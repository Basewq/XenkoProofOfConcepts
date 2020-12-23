using MultiplayerExample.Network;
using MultiplayerExample.Utilities;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Engine.Design;
using Stride.Games;
using Stride.Games.Time;
using System;
using System.Diagnostics;
using System.Reflection;

namespace MultiplayerExample.Engine
{
    abstract class GameEngineBase
    {
        private const int MaxSimulationsPerUpdate = 30;     // Max 30 simulations in a single Update call
        private static readonly TimeSpan MaximumElapsedTime = TimeSpan.FromSeconds(MaxSimulationsPerUpdate / (double)GameConfig.PhysicsSimulationRate);

        private readonly TimerTick _autoTickTimer = new TimerTick();
        private TimeSpan _previousUpdatedNonSimulationElapsedGameTime = TimeSpan.Zero;
        private bool _forceElapsedTimeToZero;

        private readonly long _targetTimeDriftAdjustmentThreshold;

        protected readonly ILogger Logger;
        protected readonly ServiceRegistry Services;
        protected readonly GameSettings Settings;
        protected readonly ContentManager Content;
        protected readonly GameSystemCollection GameSystems;

        protected readonly GameClockManager GameClockManager;

        // The GameTime for physics is faked. The real time is restricted by the PhysicsSystem, so we purposely
        // add more than the physics time to ensure it will always run the simulation.
        protected readonly GameTimeExt PhysicsGameTime = new GameTimeExt(TimeSpan.Zero, TimeSpan.Zero);

        // Many GameSystems are only called once per engine Update. In those systems we want to pass a
        // GameTime with the full elapsed time between each engine Update call, because the 'standard' UpdateTime
        // may be passed multiple times if multiple simulation updates are needed, and UpdateTime will
        // only have at most one simulation time step for the elapsed time per simulation update.
        protected readonly GameTimeExt SingleCallSystemsGameTime = new GameTimeExt(TimeSpan.Zero, TimeSpan.Zero);

        public readonly GameTimeExt UpdateTime = new GameTimeExt();

        /// <summary>
        /// Gets a value indicating whether this instance is exiting.
        /// </summary>
        /// <value><c>true</c> if this instance is exiting; otherwise, <c>false</c>.</value>
        public bool IsExiting { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        public bool IsRunning { get; protected set; }

        public bool IsFullyShutDown => IsExiting && IsRunning;

        public GameEngineBase(ContentManager contentManager, IServiceRegistry services, GameSystemCollection gameSystems = null)
        {
            Logger = GlobalLogger.GetLogger(GetType().GetTypeInfo().Name);

            GameClockManager = new GameClockManager(PhysicsGameTime);
            _targetTimeDriftAdjustmentThreshold = GameClockManager.SimulationDeltaTime.Ticks / 10;     // 10% of a single sim step

            Services = (ServiceRegistry)services;
            Content = contentManager;
            GameSystems = gameSystems ?? new GameSystemCollection(Services);

            // Replacing existing IGameSystemCollection with our own
            var existingGameSystems = Services.GetService<IGameSystemCollection>();
            if (existingGameSystems != null)
            {
                Services.RemoveService<IGameSystemCollection>();
            }
            Services.AddOrOverwriteService<IGameSystemCollection>(GameSystems);

            var networkAssetDatabase = new NetworkAssetDatabase(Content, assetFolderUrls: new[] { "Prefabs", "Scenes" });
            Services.AddOrOverwriteService(networkAssetDatabase);

            Services.AddOrOverwriteService(GameClockManager);

            var gameSettingsService = services.GetSafeServiceAs<IGameSettingsService>();
            Settings = gameSettingsService.Settings;
        }

        public void Initialize()
        {
            OnInitialize();
        }

        protected abstract void OnInitialize();

        public abstract void InitialUpdate();

        public void LoadContent()
        {
            OnLoadContent();
        }

        protected virtual void OnLoadContent() { }

        public void Update()
        {
            try
            {
                // Update the timer
                _autoTickTimer.Tick();

                var elapsedAdjustedTime = _autoTickTimer.ElapsedTimeWithPause;
                if (GameClockManager.NetworkServerSimulationClock.IsEnabled)
                {
                    // Network clock uses the full elapsed time
                    GameClockManager.NetworkServerSimulationClock.CurrentTickTimeElapsed += elapsedAdjustedTime;
                    GameClockManager.NetworkServerSimulationClock.TargetTotalTime += elapsedAdjustedTime;

                    var nextSimTotalTime = GameClockManager.SimulationClock.TotalTime + elapsedAdjustedTime;
                    var targetTimeDiff = GameClockManager.NetworkServerSimulationClock.TargetTotalTime - nextSimTotalTime;
                    // If targetTimeDiff > 0, our clock is too slow (ie. we're behind the target)
                    // If targetTimeDiff < 0, our clock is too fast (ie. we're ahead of the target)
                    var timeOffsetTicks = targetTimeDiff.Ticks;
                    if (Math.Abs(timeOffsetTicks) > _targetTimeDriftAdjustmentThreshold)
                    {
                        timeOffsetTicks /= 10;      // Only take 10% of the diff to reduce it jumping too much
                    }
                    var elapsedAdjustedTimeTicks = Math.Max(0, elapsedAdjustedTime.Ticks + timeOffsetTicks);    // Ensure elapsed time is never negative/backwards
                    elapsedAdjustedTime = TimeSpan.FromTicks(elapsedAdjustedTimeTicks);
                }
                if (_forceElapsedTimeToZero)
                {
                    elapsedAdjustedTime = TimeSpan.Zero;
                    _forceElapsedTimeToZero = false;
                }
                if (elapsedAdjustedTime > MaximumElapsedTime)
                {
                    elapsedAdjustedTime = MaximumElapsedTime;
                }

                // If the rounded targetElapsedTime is equivalent to current ElapsedAdjustedTime
                // then make ElapsedAdjustedTime = TargetElapsedTime. We take the same internal rules as XNA
                var targetElapsedTime = GameClockManager.SimulationDeltaTime;
                var targetElapsedTimeTicks = targetElapsedTime.Ticks;
                if (Math.Abs(elapsedAdjustedTime.Ticks - targetElapsedTimeTicks) < (targetElapsedTimeTicks >> 6))
                {
                    elapsedAdjustedTime = targetElapsedTime;
                }

                var updatableElapsedTimeRemaining = elapsedAdjustedTime + _previousUpdatedNonSimulationElapsedGameTime;     // Technically we are allowing to go a little over the constrained limit...
                int simUpdateCount = (int)(updatableElapsedTimeRemaining.Ticks / targetElapsedTimeTicks);
                int simUpdateCountRemaining = simUpdateCount;
                var singleFrameElapsedTime = GameClockManager.SimulationDeltaTime;
                bool updateSingleCallSystems = true;
                var singleCallSystemsElapsedTime = elapsedAdjustedTime;
                SingleCallSystemsGameTime.Update(SingleCallSystemsGameTime.Total + singleCallSystemsElapsedTime, singleCallSystemsElapsedTime, incrementFrameCount: true);
                if (_previousUpdatedNonSimulationElapsedGameTime > TimeSpan.Zero && simUpdateCountRemaining > 0)
                {
                    // Special case update - There was a partial (non-simulated) update in the previous Update call,
                    // this update is the remainder of the update which is also simulation update.
                    var prevFrameSimElapsedTime = singleFrameElapsedTime - _previousUpdatedNonSimulationElapsedGameTime;
                    Debug.Assert(prevFrameSimElapsedTime.Ticks > 0);
                    UpdateForTimeElapsed(prevFrameSimElapsedTime, currentTickTimeElapsed: TimeSpan.Zero, updatePhysicsSimulation: true, ref updateSingleCallSystems);
                    updatableElapsedTimeRemaining -= singleFrameElapsedTime;        // Remove a full update's worth because this was added from _previousUpdatedNonSimulationElapsedGameTime
                    _previousUpdatedNonSimulationElapsedGameTime = TimeSpan.Zero;
                    simUpdateCountRemaining--;
                }
                if (simUpdateCountRemaining > 0)
                {
                    // Additional full simulation update(s). Multiple updates may occur if the engine needs to
                    // catch up to the target time, or the game froze for some reason.
                    UpdateForTimeElapsed(singleFrameElapsedTime, currentTickTimeElapsed: TimeSpan.Zero, updatePhysicsSimulation: true, ref updateSingleCallSystems, updateCount: simUpdateCountRemaining);
                    updatableElapsedTimeRemaining -= TimeSpan.FromTicks(singleFrameElapsedTime.Ticks * simUpdateCountRemaining);
                }
                else if (simUpdateCountRemaining == 0 && _previousUpdatedNonSimulationElapsedGameTime > TimeSpan.Zero)
                {
                    // Remove the previous updated time, otherwise we double up on the update
                    updatableElapsedTimeRemaining -= _previousUpdatedNonSimulationElapsedGameTime;
                }
                //if (updatableElapsedTimeRemaining > GameUpdateMinimumNonSimulationElapsedTime || simUpdateCount == 0)
                if (updatableElapsedTimeRemaining > TimeSpan.Zero)
                {
                    // Do a non-simulation update because this Update call isn't hasn't
                    // elapsed enough for a simulation, or there is still some time remaining.
                    // The purpose of this is to make the rendering time closer to the actual time elapsed,
                    // or ensure at least one engine update call is done per Update call.
                    var currentTickTimeElapsed = updatableElapsedTimeRemaining + _previousUpdatedNonSimulationElapsedGameTime;
                    UpdateForTimeElapsed(updatableElapsedTimeRemaining, currentTickTimeElapsed, updatePhysicsSimulation: false, ref updateSingleCallSystems);
                    _previousUpdatedNonSimulationElapsedGameTime = currentTickTimeElapsed;
                }

                GameSystemsPostUpdate();
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected exception.", ex);
                throw;
            }
        }

        /// <param name="elapsedTimePerUpdate">
        /// The amount of time passed between each update of the game's system.
        /// </param>
        /// <param name="updateCount">
        /// The amount of updates that will be executed on the game's systems.
        /// </param>
        private void UpdateForTimeElapsed(TimeSpan elapsedTimePerUpdate, TimeSpan currentTickTimeElapsed, bool updatePhysicsSimulation, ref bool updateSingleCallSystems, int updateCount = 1)
        {
            try
            {
#if DEBUG
                //var context = Services.GetService<GameEngineContext>();
                //if (context.IsClient)
                //{
                //    System.Diagnostics.Debug.WriteLine(@$">>UpdateForTimeElapsed: elapsedTimePerUpdate {elapsedTimePerUpdate} - currentTickTimeElapsed {currentTickTimeElapsed} - updatePhysicsSimulation {updatePhysicsSimulation} - updateCount {updateCount}");
                //}
#endif
                for (int i = 0; i < updateCount && !IsExiting; i++)
                {
                    UpdateTime.Update(UpdateTime.Total + elapsedTimePerUpdate, elapsedTimePerUpdate, incrementFrameCount: true);
                    //Console.WriteLine($"GameTime Total: {UpdateTime.Total.TotalMilliseconds} - Elapsed: {elapsedTimePerUpdate.TotalMilliseconds}");
                    using (Profiler.Begin(GameProfilingKeys.GameUpdate))
                    {
                        InternalUpdate(UpdateTime, updatePhysicsSimulation, currentTickTimeElapsed, updateSingleCallSystems);
                        updateSingleCallSystems = false;
                    }
                }
            }
            finally
            {
                CheckEndRun();
            }
        }

        private void CheckEndRun()
        {
            if (IsExiting && IsRunning)
            {
                EndRun();
                IsRunning = false;
            }
        }

        private void InternalUpdate(GameTimeExt gameTime, bool updatePhysicsSimulation, TimeSpan currentTickTimeElapsed, bool updateSingleCallSystems)
        {
            if (updatePhysicsSimulation)
            {
                // Update the physics time (which is in fixed time steps)
                PhysicsGameTime.Update(PhysicsGameTime.Total + GameClockManager.SimulationDeltaTime, GameClockManager.SimulationDeltaTime, incrementFrameCount: true);
            }
            if (GameClockManager.SimulationClock.IsEnabled)
            {
                GameClockManager.SimulationClock.CurrentTickTimeElapsed = currentTickTimeElapsed;
                GameClockManager.SimulationClock.TotalTime += gameTime.Elapsed;
                GameClockManager.SimulationClock.IsNextSimulation = updatePhysicsSimulation;
                if (updatePhysicsSimulation)
                {
                    GameClockManager.SimulationClock.SimulationTickNumber++;
                    //System.Diagnostics.Debug.WriteLine($"UPDATE: TICK: {_gameClockManager.SimulationTickNumber}");
                }
#if DEBUG
                //var context = Services.GetService<GameEngineContext>();
                //if (context.IsClient)
                //{
                //    System.Diagnostics.Debug.WriteLine(@$">>SimTick {GameClockManager.SimulationClock.SimulationTickNumber} - SimTotalTime {GameClockManager.SimulationClock.TotalTime} - SimCurElapsed {GameClockManager.SimulationClock.CurrentTickTimeElapsed}");
                //}
#endif
            }
            GameSystemsUpdate(updatePhysicsSimulation, updateSingleCallSystems);
        }

        protected abstract void GameSystemsUpdate(bool updatePhysicsSimulation, bool updateSingleCallSystems);

        protected virtual void GameSystemsPostUpdate() { }

        public virtual bool BeginDraw() => false;

        public virtual void Draw() { }

        public virtual void EndDraw() { }

        /// <summary>
        /// Resets the elapsed time counter.
        /// </summary>
        public void ResetElapsedTime()
        {
            _forceElapsedTimeToZero = true;
        }

        /// <summary>
        /// Called after all components are initialized, before the game loop starts.
        /// </summary>
        protected virtual void BeginRun() { }

        /// <summary>Called after the game loop has stopped running before exiting.</summary>
        protected virtual void EndRun() { }

        /// <summary>
        /// Creates the game system key value from the GameSystems collection if it exists, or creates a game system.
        /// </summary>
        protected GameSystemKeyValue<T> CreateKeyValue<T>(Func<T> createGameSystem) where T : GameSystemBase
        {
            T gameSystem = null;
            for (int i = 0; i < GameSystems.Count; i++)
            {
                if (GameSystems[i] is T existingGameSystem)
                {
                    gameSystem = existingGameSystem;
                    break;
                }
            }
            gameSystem ??= createGameSystem();

            var gameSystemKey = new ProfilingKey(GameProfilingKeys.GameUpdate, gameSystem.GetType().Name);
            return new GameSystemKeyValue<T>(gameSystemKey, gameSystem);
        }

        /// <summary>
        /// Creates the game system key value from the supplied game system..
        /// </summary>
        protected static GameSystemKeyValue<T> CreateKeyValue<T>(T gameSystem) where T : GameSystemBase
        {
            var gameSystemKey = new ProfilingKey(GameProfilingKeys.GameUpdate, gameSystem.GetType().Name);
            return new GameSystemKeyValue<T>(gameSystemKey, gameSystem);
        }
    }
}
