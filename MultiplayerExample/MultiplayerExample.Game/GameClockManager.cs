using MultiplayerExample.Core;
using System;

namespace MultiplayerExample
{
    class GameClockManager
    {
        /// <summary>
        /// The simulation clock on the local machine.
        /// </summary>
        public SimulationClock SimulationClock;

        /// <summary>
        /// The clock based on last received server packets.
        /// Only applicable for game clients.
        /// </summary>
        public NetworkServerSimulationClock NetworkServerSimulationClock;

        public TimeSpan RemoteEntityRenderTimeDelay = TimeSpan.FromMilliseconds(100);

        public static TimeSpan CalculateTickTimeElapsed(TimeSpan totalTime, SimulationTickNumber simulationTickNumber)
        {
            var simTickTime = TimeSpan.FromTicks(simulationTickNumber * GameConfig.PhysicsFixedTimeStep.Ticks);
            var timeElapsed = totalTime - simTickTime;
            return timeElapsed;
        }

        public static SimulationTickNumber CalculateSimulationTickNumber(TimeSpan totalTime)
        {
            // This always rounds down.
            var simTickNo = new SimulationTickNumber(totalTime.Ticks / GameConfig.PhysicsFixedTimeStep.Ticks);
            return simTickNo;
        }
    }

    struct SimulationClock
    {
        public bool IsEnabled;

        /// <summary>
        /// The current tick number of the simulation.
        /// </summary>
        public SimulationTickNumber SimulationTickNumber;

        /// <summary>
        /// The current time elapsed for the current simulation tick.
        /// </summary>
        public TimeSpan CurrentTickTimeElapsed;

        /// <summary>
        /// Total time since the start of the simulation.
        /// <br />
        /// Equivalent to (<see cref="GameConfig.PhysicsFixedTimeStep"/> * <see cref="SimulationTickNumber"/>) + <see cref="CurrentTickTimeElapsed"/>.
        /// </summary>
        public TimeSpan TotalTime;

        /// <summary>
        /// True if this is currently a new simulation tick compared to the last update call.
        /// </summary>
        /// <remarks>
        /// This is a minor hack, but some processors/systems need to know if their current Update call
        /// is part of a new simulation step to ensure it is in sync with the physics step.
        /// </remarks>
        public bool IsNextSimulation;

        internal void Reset()
        {
            SimulationTickNumber = default;
            CurrentTickTimeElapsed = TimeSpan.Zero;
            TotalTime = TimeSpan.Zero;
        }
    }

    struct NetworkServerSimulationClock
    {
        public bool IsEnabled;
        /// <summary>
        /// The last received simulation tick from the server.
        /// </summary>
        public SimulationTickNumber LastServerSimulationTickNumber;
        /// <summary>
        /// The current time elapsed for the last received simulation tick from the server.
        /// This is always reset to zero when a new <see cref="LastServerSimulationTickNumber"/> is received.
        /// </summary>
        public TimeSpan CurrentTickTimeElapsed;

        /// <summary>
        /// Total time since the start of the simulation based on the last received simulation tick from the server.
        /// <br />
        /// Equivalent to (<see cref="GameConfig.PhysicsFixedTimeStep"/> * <see cref="LastServerSimulationTickNumber"/>) + <see cref="CurrentTickTimeElapsed"/>.
        /// <br />
        /// This time may jitter (ie. increase or decrease) since it is recalculated whenever <see cref="LastServerSimulationTickNumber"/> is changed,
        /// so should only be used as a reference for adjusting the 'real' simulation clock on the client.
        /// </summary>
        public TimeSpan TargetTotalTime;

        public void SetClockFromTickNumber(SimulationTickNumber simulationTickNumber)
        {
            LastServerSimulationTickNumber = simulationTickNumber;
            CurrentTickTimeElapsed = TimeSpan.Zero;
            TargetTotalTime = TimeSpan.FromTicks(simulationTickNumber * GameConfig.PhysicsFixedTimeStep.Ticks);
        }
    }
}
