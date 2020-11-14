using MultiplayerExample.Core;
using MultiplayerExample.Data;
using MultiplayerExample.Engine;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using System;

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
                ModelChildEntity = entity.GetChild(0),
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
            MovementSnapshotsComponent movementSnapshotsComp,
            TransformComponent transform,
            TransformComponent modelChildTransform)
        {
            predictedMovements.Add(default);
            ref var nextMovementData = ref predictedMovements.Items[predictedMovements.Count - 1];

            var movementSnapshotStore = movementSnapshotsComp.SnapshotStore;
            if (predictedMovements.Count >= 2)
            {
                // Just copy the previous snapshot's data
                nextMovementData = predictedMovements.Items[predictedMovements.Count - 2];
            }
            else if (movementSnapshotStore != null && movementSnapshotStore.Count > 0)  // movementSnapshotStore doesn't exist on newly created entities
            {
                // Copy the last 'real' movement data
                ref var movementData = ref movementSnapshotsComp.SnapshotStore.GetLatest();
                nextMovementData = movementData;
            }
            else
            {
                // New data without any previous information
                nextMovementData.MoveDirection = Vector3.Zero;
                var faceDir = -Vector3.UnitZ;
                nextMovementData.YawOrientation = MathUtil.RadiansToDegrees((float)Math.Atan2(faceDir.Z, faceDir.X) + MathUtil.PiOverTwo);
                nextMovementData.JumpReactionRemaining = movementSnapshotsComp.JumpReactionThreshold;
                nextMovementData.LocalPosition = transform.Position;
                nextMovementData.LocalRotation = modelChildTransform.Rotation;
                nextMovementData.PhysicsEngineLinearVelocity = Vector3.Zero;
            }
            nextMovementData.SimulationTickNumber = simulationTickNumber;
            nextMovementData.SnapshotType = SnapshotType.ClientPrediction;  // Technically this will always be ClientPrediction, but just set it anyway...

            return ref nextMovementData;
        }

        internal class AssociatedData
        {
            internal ClientPredictionSnapshotsComponent ClientPredictionSnapshotsComponent;
            internal MovementSnapshotsComponent MovementSnapshotsComponent;

            internal TransformComponent TransformComponent;
            internal Entity ModelChildEntity;
        }
    }
}
