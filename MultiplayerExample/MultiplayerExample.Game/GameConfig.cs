using System;

namespace MultiplayerExample
{
    static class GameConfig
    {
        /// <summary>
        /// Number of simulations to run per second.
        /// </summary>
        public const int PhysicsSimulationRate = 30;

        // Note: do not use TimeSpan.FromSeconds because it is less precise in calculating the time for some reason.
        public static readonly TimeSpan PhysicsFixedTimeStep = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / PhysicsSimulationRate);
    }
}
