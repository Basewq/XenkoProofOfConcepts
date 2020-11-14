using MultiplayerExample.Core;
using MultiplayerExample.Data;
using MultiplayerExample.Engine;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Physics;
using System;
using System.Diagnostics;

namespace MultiplayerExample.Network.SnapshotStores
{
    /// <summary>
    /// This processor stores and restores the entity's position data (from/to the standard Stride entity system)
    /// using the snapshot buffer.
    /// </summary>
    class MovementSnapshotsProcessor : EntityProcessor<MovementSnapshotsComponent, MovementSnapshotsProcessor.AssociatedData>,
        IInGameProcessor, IPreUpdateProcessor, IPostUpdateProcessor
    {
        private static readonly ObjectPool<SnapshotStore<MovementSnapshotsComponent.MovementData>> ObjectPool
            = new ObjectPool<SnapshotStore<MovementSnapshotsComponent.MovementData>>(
                initialCapacity: 64,
                objectCreatorFunc: () => new SnapshotStore<MovementSnapshotsComponent.MovementData>(SnapshotBufferSize),
                onObjectPutAction: ObjectPoolItemClearer<SnapshotStore<MovementSnapshotsComponent.MovementData>>.Default);

        public const float SnapshotDurationInSeconds = 5f;
        public const int SnapshotBufferSize = (int)(GameConfig.PhysicsSimulationRate * SnapshotDurationInSeconds + 0.5f);   // Always round up

        private GameClockManager _gameClockManager;
        private GameEngineContext _gameEngineContext;

        public bool IsEnabled { get; set; }

        public MovementSnapshotsProcessor()
        {
            Order = -1000;
            // Not using Enabled property, because that completely disables the processor, where it doesn't even pick up newly added entities
            IsEnabled = true;
        }

        protected override void OnSystemAdd()
        {
            _gameClockManager = Services.GetService<GameClockManager>();
            _gameEngineContext = Services.GetService<GameEngineContext>();
        }

        protected override AssociatedData GenerateComponentData([NotNull] Entity entity, [NotNull] MovementSnapshotsComponent component)
        {
            return new AssociatedData
            {
                // Can also add other info/components here
                TransformComponent = entity.Transform,
                ModelChildTransform = entity.GetChild(0).Transform,

                CharacterComponent = entity.Get<CharacterComponent>(),
                ClientPredictionSnapshotsComponent = entity.Get<ClientPredictionSnapshotsComponent>()
            };
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] MovementSnapshotsComponent component, [NotNull] AssociatedData data)
        {
            component.SnapshotStore = ObjectPool.GetObject();
            //var simTickNumber = _gameClockManager.SimulationClock.SimulationTickNumber;
            //CreateNewSnapshotData(component.SnapshotStore, simTickNumber, component, data.TransformComponent, data.ModelChildTransform);
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] MovementSnapshotsComponent component, [NotNull] AssociatedData data)
        {
            ObjectPool.PutObject(component.SnapshotStore);
            component.SnapshotStore = null;
            data.TransformComponent = null;
            data.ModelChildTransform = null;
            data.ClientPredictionSnapshotsComponent = null;
        }

        protected override bool IsAssociatedDataValid([NotNull] Entity entity, [NotNull] MovementSnapshotsComponent component, [NotNull] AssociatedData associatedData)
        {
            return associatedData.TransformComponent == entity.Transform
                && associatedData.ModelChildTransform == entity.GetChild(0).Transform
                && associatedData.CharacterComponent == entity.Get<CharacterComponent>()
                && associatedData.ClientPredictionSnapshotsComponent == entity.Get<ClientPredictionSnapshotsComponent>();
        }

        public void PreUpdate(GameTime gameTime)
        {
            var simTickNumber = _gameClockManager.SimulationClock.SimulationTickNumber;

            if (_gameEngineContext.IsServer)
            {
                if (_gameClockManager.SimulationClock.IsNextSimulation)
                {
                    // Only generate new snapshot during a new simulation update
                    foreach (var kv in ComponentDatas)
                    {
                        var movementSnapshotsComp = kv.Key;
                        var data = kv.Value;
                        CreateNewSnapshotData(movementSnapshotsComp.SnapshotStore, simTickNumber, movementSnapshotsComp, data.TransformComponent, data.ModelChildTransform);
                    }
                }
                // Server only needs to create the snapshot data
                return;
            }

            // Update the current entity transforms to the latest for the physics engine to read and write.
            // This is because MovementSnapshotsRenderProcessor changes the transform during Draw for interpolation which we need to undo.
            foreach (var kv in ComponentDatas)
            {
                var movementSnapshotsComp = kv.Key;
                if (movementSnapshotsComp.SnapshotStore.IsEmpty)
                {
                    continue;
                }
                var data = kv.Value;

                var predictedMovements = data.ClientPredictionSnapshotsComponent?.PredictedMovements;
                if (predictedMovements?.Count > 0)
                {
                    // Use the predicted positions
                    ref var movementData = ref predictedMovements.Items[predictedMovements.Count - 1];
                    data.TransformComponent.Position = movementData.LocalPosition;
                    data.ModelChildTransform.Rotation = movementData.LocalRotation;
                    if (data.CharacterComponent != null)
                    {
                        data.TransformComponent.UpdateWorldMatrix();
                        data.CharacterComponent.UpdatePhysicsTransformation();
                    }
                }
                else
                {
                    ref var movementData = ref movementSnapshotsComp.SnapshotStore.GetLatest();  // There should always be at least one snapshot generated.
                    data.TransformComponent.Position = movementData.LocalPosition;
                    data.ModelChildTransform.Rotation = movementData.LocalRotation;
                    if (data.CharacterComponent != null)
                    {
                        data.TransformComponent.UpdateWorldMatrix();
                        data.CharacterComponent.UpdatePhysicsTransformation();
                    }
                }
            }
        }

        public void PostUpdate(GameTime gameTime)
        {
            var simTickNumber = _gameClockManager.SimulationClock.SimulationTickNumber;
            // Update the transforms to the latest for other processors to read.
            // This occurs AFTER the every thing.

            if (!_gameClockManager.SimulationClock.IsNextSimulation)
            {
                // Saving movement data only needs to occur in simulation step.
                return;
            }

            if (_gameEngineContext.IsClient)
            {
                foreach (var kv in ComponentDatas)
                {
                    var data = kv.Value;
                    var clientPredictionSnapshotsComp = data.ClientPredictionSnapshotsComponent;
                    if (clientPredictionSnapshotsComp == null)
                    {
                        continue;
                    }
                    var predictedMovements = clientPredictionSnapshotsComp.PredictedMovements;
                    ref var predictedMovementData = ref predictedMovements.Items[predictedMovements.Count - 1];     // Data was already created in MovementSnapshotsInputProcessor.Update, just need to save the position data
                    Debug.Assert(predictedMovementData.SimulationTickNumber == simTickNumber);

                    // Save resulting position in the predicted movement buffer
                    predictedMovementData.LocalPosition = data.TransformComponent.Position;
                    predictedMovementData.LocalRotation = data.ModelChildTransform.Rotation;
                    predictedMovementData.PhysicsEngineLinearVelocity = BulletPhysicsExt.GetLinearVelocity(data.CharacterComponent);

#if DEBUG
                    //Debug.WriteLine(@$"Sim: {predictedMovementData.SimulationTickNumber} Pos: {predictedMovementData.LocalPosition}");
#endif
                }
            }
            else
            {
                foreach (var kv in ComponentDatas)
                {
                    var movementSnapshotsComp = kv.Key;
                    if (movementSnapshotsComp.SnapshotStore.Count < 2)
                    {
                        continue;
                    }
                    var data = kv.Value;

                    var findResult = movementSnapshotsComp.SnapshotStore.TryFindSnapshot(simTickNumber);
                    Debug.Assert(findResult.IsFound);
                    // Save resulting position in the movement snapshot buffer
                    ref var movementData = ref findResult.Result;
                    movementData.LocalPosition = data.TransformComponent.Position;
                    movementData.LocalRotation = data.ModelChildTransform.Rotation;
                    movementData.PhysicsEngineLinearVelocity = BulletPhysicsExt.GetLinearVelocity(data.CharacterComponent);
#if DEBUG
                    //Debug.WriteLine(@$"--FromSvr Pos: {movementData.LocalPosition} - Sim: {movementData.SimulationTickNumber} - PIDApplied {movementData.PlayerInputSequenceNumberApplied} - MvDir {movementData.MoveDirection}");
                    Debug.WriteLine(@$"--FromSvr Pos: {movementData.LocalPosition} - Sim: {movementData.SimulationTickNumber} - PIDApplied {movementData.PlayerInputSequenceNumberApplied} - MvDir {movementData.MoveDirection} - IsGrounded {movementData.IsGrounded} - Vel {movementData.PhysicsEngineLinearVelocity}");
                    if (movementData.MoveDirection.X != 0)
                    {
                    }
#endif
                }
            }
        }

        private void CreateNewSnapshotData(
            SnapshotStore<MovementSnapshotsComponent.MovementData> snapshotStore,
            SimulationTickNumber simulationTickNumber,
            MovementSnapshotsComponent component,
            TransformComponent transform,
            TransformComponent modelChildTransform)
        {
            ref var movementData = ref snapshotStore.GetOrCreate(simulationTickNumber);
            if (_gameEngineContext.IsServer || movementData.SnapshotType != SnapshotType.Server)
            {
                var prevMovementFindResult = snapshotStore.TryFindSnapshot(simulationTickNumber - 1);
                if (prevMovementFindResult.IsFound)
                {
                    // Just copy the previous snapshot's data
                    movementData = prevMovementFindResult.Result;
                }
                else
                {
                    movementData.MoveDirection = Vector3.Zero;
                    var faceDir = -Vector3.UnitZ;
                    movementData.YawOrientation = MathUtil.RadiansToDegrees((float)Math.Atan2(faceDir.Z, faceDir.X) + MathUtil.PiOverTwo);
                    movementData.JumpReactionRemaining = component.JumpReactionThreshold;
                    movementData.LocalPosition = transform.Position;
                    movementData.LocalRotation = modelChildTransform.Rotation;
                    movementData.PhysicsEngineLinearVelocity = Vector3.Zero;
                }
                movementData.SimulationTickNumber = simulationTickNumber;
                movementData.SnapshotType = _gameEngineContext.IsServer ? SnapshotType.Server : SnapshotType.ClientPrediction;
            }
        }

        internal class AssociatedData
        {
            internal TransformComponent TransformComponent;
            internal TransformComponent ModelChildTransform;

            internal CharacterComponent CharacterComponent;
            internal ClientPredictionSnapshotsComponent ClientPredictionSnapshotsComponent;     // Optional
        }
    }
}
