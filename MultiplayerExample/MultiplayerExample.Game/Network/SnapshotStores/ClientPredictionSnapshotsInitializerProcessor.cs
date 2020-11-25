using MultiplayerExample.Core;
using MultiplayerExample.Data;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Engine;

namespace MultiplayerExample.Network.SnapshotStores
{
    class ClientPredictionSnapshotsInitializerProcessor : EntityProcessor<ClientPredictionSnapshotsComponent, ClientPredictionSnapshotsInitializerProcessor.AssociatedData>
    {
        private static readonly ObjectPool<FastList<MovementSnapshotsComponent.MovementData>> ObjectPool
            = new ObjectPool<FastList<MovementSnapshotsComponent.MovementData>>(
                initialCapacity: 4,
                objectCreatorFunc: () => new FastList<MovementSnapshotsComponent.MovementData>(10),
                onObjectPutAction: x => x.Clear());

        protected override AssociatedData GenerateComponentData([NotNull] Entity entity, [NotNull] ClientPredictionSnapshotsComponent component)
        {
            return new AssociatedData
            {
                ClientPredictionSnapshotsComponent = component,
                // Can also add other info/components here
                MovementSnapshotsComponent = entity.Get<MovementSnapshotsComponent>(),
                TransformComponent = entity.Transform,
            };
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] ClientPredictionSnapshotsComponent component, [NotNull] AssociatedData data)
        {
            component.PredictedMovements = ObjectPool.GetObject();
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ClientPredictionSnapshotsComponent component, [NotNull] AssociatedData data)
        {
            ObjectPool.PutObject(component.PredictedMovements);
            component.PredictedMovements = null;
        }

        protected override bool IsAssociatedDataValid([NotNull] Entity entity, [NotNull] ClientPredictionSnapshotsComponent component, [NotNull] AssociatedData associatedData)
        {
            // Check the all the components are still the same.
            // This can fail if any component is removed from the entity.
            return associatedData.ClientPredictionSnapshotsComponent == component;
        }

        internal static ref MovementSnapshotsComponent.MovementData CreateNewSnapshotData(
            FastList<MovementSnapshotsComponent.MovementData> predictedMovements,
            SimulationTickNumber simulationTickNumber,
            MovementSnapshotsComponent movementSnapshotsComponent,
            TransformComponent transformComponent)
        {
            predictedMovements.Add(default);
            ref var nextMovementData = ref predictedMovements.Items[predictedMovements.Count - 1];

            var movementSnapshotStore = movementSnapshotsComponent.SnapshotStore;
            if (predictedMovements.Count >= 2)
            {
                // Just copy the previous snapshot's data
                nextMovementData = predictedMovements.Items[predictedMovements.Count - 2];
            }
            else if (movementSnapshotStore != null && movementSnapshotStore.Count > 0)  // movementSnapshotStore doesn't exist on newly created entities
            {
                // Copy the last 'real' movement data
                ref var movementData = ref movementSnapshotsComponent.SnapshotStore.GetLatest();
                nextMovementData = movementData;
            }
            else
            {
                // New data without any previous information
                MovementSnapshotsComponent.InitializeNewMovementData(ref nextMovementData, transformComponent.Position, movementSnapshotsComponent.JumpReactionThreshold);
            }
            nextMovementData.SimulationTickNumber = simulationTickNumber;

            return ref nextMovementData;
        }

        internal class AssociatedData
        {
            internal ClientPredictionSnapshotsComponent ClientPredictionSnapshotsComponent;
            internal MovementSnapshotsComponent MovementSnapshotsComponent;

            internal TransformComponent TransformComponent;
        }
    }
}
