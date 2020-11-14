using System;

namespace MultiplayerExample
{
    static class GameConfig
    {
        /// <summary>
        /// Number of simulations to run per second.
        /// </summary>
        public const int PhysicsSimulationRate = 30;

        public static readonly TimeSpan PhysicsFixedTimeStep = TimeSpan.FromSeconds(1.0 / PhysicsSimulationRate);
    }
}
