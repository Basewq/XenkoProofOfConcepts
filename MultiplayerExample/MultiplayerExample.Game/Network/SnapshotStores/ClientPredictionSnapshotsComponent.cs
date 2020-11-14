using Stride.Core;
using Stride.Core.Collections;
using Stride.Engine;
using Stride.Engine.Design;

namespace MultiplayerExample.Network.SnapshotStores
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ClientPredictionSnapshotsInitializerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    public class ClientPredictionSnapshotsComponent : EntityComponent
    {
        internal FastList<MovementSnapshotsComponent.MovementData> PredictedMovements;
    }
}
