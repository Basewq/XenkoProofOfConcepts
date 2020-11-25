using MultiplayerExample.Network;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Core.Streaming;
using Stride.Engine.Design;
using Stride.Games;
using Stride.Games.Time;
using Stride.Graphics.Data;
using Stride.Streaming;
using System;
using System.Reflection;

namespace MultiplayerExample.Engine
{
    abstract class GameEngineBase
    {
        private readonly IServiceRegistry _globalServices;

        private readonly TimerTick _autoTickTimer = new TimerTick();
        private const int MaxSimulationsPerUpdate = 30;     // Max 30 simulations in a single Update call
        private readonly TimeSpan _maximumElapsedTime = TimeSpan.FromSeconds(MaxSimulationsPerUpdate / (double)GameConfig.PhysicsSimulationRate);
        private TimeSpan _simulationAccumulatedElapsedGameTime;
        private TimeSpan _nonSimulationAccumulatedElapsedGameTime;
        private bool _previousUpdateWasSimulationUpdate;
        private bool _forceElapsedTimeToZero;

        private readonly long _targetTimeDriftAdjustmentThreshold = GameConfig.PhysicsFixedTimeStep.Ticks / 10;     // 10% of a single sim step

        protected readonly ILogger Logger;
        protected readonly ServiceRegistry Services;
        protected readonly GameSettings Settings;
        protected readonly ContentManager Content;
        protected readonly GameSystemCollection GameSystems;

        protected readonly GameClockManager GameClockManager = new GameClockManager();

        // The GameTime for physics is faked. The real time is restricted by the PhysicsSystem, so we purposely
        // add more than the physics time to ensure it will always run the simulation.
        protected readonly GameTimeExt PhysicsGameTime = new GameTimeExt(TimeSpan.Zero, TimeSpan.Zero);

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

        public GameEngineBase(ContentManager contentManager, IServiceRegistry globalServices)
        {
            _globalServices = globalServices;

            Logger = GlobalLogger.GetLogger(GetType().GetTypeInfo().Name);

            Services = new ServiceRegistry();
            Content = contentManager;
            GameSystems = new GameSystemCollection(Services);

            // This is a bit of a implementation detail hack, but necessary so that we don't
            // blow out the memory if multiple 'engines' are loading the same content.
            Services.AddService<IContentManager>(Content);
            Services.AddService(Content);

            Services.AddService(globalServices.GetSafeServiceAs<IExitGameService>());
            Services.AddService(globalServices.GetSafeServiceAs<IDatabaseFileProviderService>());

            Services.AddService(globalServices.GetSafeServiceAs<StreamingManager>());
            Services.AddService(globalServices.GetSafeServiceAs<IStreamingManager>());
            Services.AddService(globalServices.GetSafeServiceAs<ITexturesStreamingProvider>());

            Services.AddService(globalServices.GetSafeServiceAs<NetworkAssetDatabase>());

            Services.AddService(GameClockManager);

            var gameSettingsService = globalServices.GetSafeServiceAs<IGameSettingsService>();
            Services.AddService(gameSettingsService);
            Settings = gameSettingsService.Settings;
        }

        public void Initialize()
        {
            OnInitialize(_globalServices);
        }

        protected abstract void OnInitialize(IServiceRegistry globalServices);

        public abstract void InitialUpdate();

        public void LoadContent()
        {
            GameSystems.LoadContent();
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
                if (elapsedAdjustedTime > _maximumElapsedTime)
                {
                    elapsedAdjustedTime = _maximumElapsedTime;
                }

                // If the rounded targetElapsedTime is equivalent to current ElapsedAdjustedTime
                // then make ElapsedAdjustedTime = TargetElapsedTime. We take the same internal rules as XNA
                var targetElapsedTime = GameConfig.PhysicsFixedTimeStep;
                var targetElapsedTimeTicks = targetElapsedTime.Ticks;
                if (Math.Abs(elapsedAdjustedTime.Ticks - targetElapsedTimeTicks) < (targetElapsedTimeTicks >> 6))
                {
                    elapsedAdjustedTime = targetElapsedTime;
                }

                var prevSimAccumulatedElapsedGameTime = _simulationAccumulatedElapsedGameTime;
                // Update the accumulated time
                _simulationAccumulatedElapsedGameTime += elapsedAdjustedTime;

                int updateCount = (int)(_simulationAccumulatedElapsedGameTime.Ticks / targetElapsedTimeTicks);
                if (updateCount >= 1)
                {
                    // There is at least one simulation to run

                    // We are going to call Update updateCount times, so we can subtract this from accumulated elapsed game time
                    _simulationAccumulatedElapsedGameTime = new TimeSpan(_simulationAccumulatedElapsedGameTime.Ticks - (updateCount * targetElapsedTimeTicks));

                    // If the last update was not a simulation update, the clock time elapsed is the amount that
                    // pushed it over the threshold.
                    // If the last update was a simulation update, the clock is currently in-synced with the physics clock,
                    // and discard the prevSimAccumulatedElapsedGameTime.
                    var firstSimUpdateTimeElapsed = _previousUpdateWasSimulationUpdate
                                                    ? GameConfig.PhysicsFixedTimeStep
                                                    : GameConfig.PhysicsFixedTimeStep - prevSimAccumulatedElapsedGameTime;
                    UpdateForTimeElapsed(firstSimUpdateTimeElapsed, currentTickTimeElapsed: TimeSpan.Zero, updatePhysicsSimulation: true);

                    if (updateCount >= 2)
                    {
                        //Console.WriteLine($"-----Too slow, updateCount: {updateCount}");
                        // These are full sim time-steps
                        var singleFrameElapsedTime = GameConfig.PhysicsFixedTimeStep;
                        UpdateForTimeElapsed(singleFrameElapsedTime, currentTickTimeElapsed: TimeSpan.Zero, updatePhysicsSimulation: true, updateCount - 1);
                    }

                    // The remaining unused time is to be used in the next update
                    _nonSimulationAccumulatedElapsedGameTime = _simulationAccumulatedElapsedGameTime;
                    _previousUpdateWasSimulationUpdate = true;
                }
                else
                {
                    UpdateForTimeElapsed(elapsedAdjustedTime + _nonSimulationAccumulatedElapsedGameTime, currentTickTimeElapsed: _nonSimulationAccumulatedElapsedGameTime, updatePhysicsSimulation: false);
                    _nonSimulationAccumulatedElapsedGameTime = TimeSpan.Zero;
                    _previousUpdateWasSimulationUpdate = false;
                }

                UpdateDrawTimer(UpdateTime);
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
        private void UpdateForTimeElapsed(TimeSpan elapsedTimePerUpdate, TimeSpan currentTickTimeElapsed, bool updatePhysicsSimulation, int updateCount = 1)
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
                        UpdateInternal(UpdateTime, updatePhysicsSimulation, currentTickTimeElapsed);
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

        private void UpdateInternal(GameTimeExt gameTime, bool updatePhysicsSimulation, TimeSpan currentTickTimeElapsed)
        {
            if (updatePhysicsSimulation)
            {
                // Update the physics time (which is in fixed time steps)
                PhysicsGameTime.Update(PhysicsGameTime.Total + GameConfig.PhysicsFixedTimeStep, GameConfig.PhysicsFixedTimeStep, incrementFrameCount: true);
            }
            if (GameClockManager.SimulationClock.IsEnabled)
            {
                GameClockManager.SimulationClock.CurrentTickTimeElapsed += gameTime.Elapsed;
                GameClockManager.SimulationClock.TotalTime += gameTime.Elapsed;
                GameClockManager.SimulationClock.IsNextSimulation = updatePhysicsSimulation;
                if (updatePhysicsSimulation)
                {
                    GameClockManager.SimulationClock.CurrentTickTimeElapsed = currentTickTimeElapsed;
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
            UpdateGameSystems(gameTime, updatePhysicsSimulation, PhysicsGameTime);
        }

        protected abstract void UpdateGameSystems(GameTimeExt gameTime, bool updatePhysicsSimulation, GameTimeExt physicsGameTime);

        protected virtual void UpdateDrawTimer(GameTime updateTime) { }

        public virtual bool BeginDraw() { return false; }

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
        protected virtual void BeginRun()
        {
        }

        /// <summary>Called after the game loop has stopped running before exiting.</summary>
        protected virtual void EndRun()
        {
        }

        protected static GameSystemKeyValue<T> CreateKeyValue<T>(T gameSystem) where T : GameSystemBase
        {
            var gameSystemKey = new ProfilingKey(GameProfilingKeys.GameUpdate, gameSystem.GetType().Name);
            return new GameSystemKeyValue<T>(gameSystemKey, gameSystem);
        }
    }
}
