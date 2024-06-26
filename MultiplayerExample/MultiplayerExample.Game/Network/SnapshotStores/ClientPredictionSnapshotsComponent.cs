using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using System.Collections.Generic;

namespace MultiplayerExample.Network.SnapshotStores
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ClientPredictionSnapshotsInitializerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    public class ClientPredictionSnapshotsComponent : EntityComponent
    {
        internal List<MovementSnapshotsComponent.MovementData> PredictedMovements;
    }
}
