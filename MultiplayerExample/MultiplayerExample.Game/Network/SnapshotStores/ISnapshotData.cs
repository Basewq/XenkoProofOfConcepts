using MultiplayerExample.Core;

namespace MultiplayerExample.Network.SnapshotStores
{
    interface ISnapshotData
    {
        /// <summary>
        /// The simulation tick number this data is on.
        /// </summary>
        public SimulationTickNumber SimulationTickNumber { get; }
    }
}
