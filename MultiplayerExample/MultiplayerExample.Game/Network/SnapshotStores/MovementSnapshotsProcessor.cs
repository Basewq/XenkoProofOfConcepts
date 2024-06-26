using MultiplayerExample.Core;
using MultiplayerExample.Data;
using MultiplayerExample.Engine;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;
using Stride.Physics;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
        private IGameNetworkService _networkService;

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
            _networkService = Services.GetService<IGameNetworkService>();
        }

        protected override AssociatedData GenerateComponentData([NotNull] Entity entity, [NotNull] MovementSnapshotsComponent component)
        {
            return new AssociatedData
            {
                // Can also add other info/components here
                TransformComponent = entity.Transform,

                CharacterComponent = entity.Get<CharacterComponent>(),
                ClientPredictionSnapshotsComponent = entity.Get<ClientPredictionSnapshotsComponent>()
            };
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] MovementSnapshotsComponent component, [NotNull] AssociatedData data)
        {
            component.SnapshotStore = ObjectPool.GetObject();
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] MovementSnapshotsComponent component, [NotNull] AssociatedData data)
        {
            ObjectPool.PutObject(component.SnapshotStore);
            component.SnapshotStore = null;
            data.TransformComponent = null;
            data.ClientPredictionSnapshotsComponent = null;
        }

        protected override bool IsAssociatedDataValid([NotNull] Entity entity, [NotNull] MovementSnapshotsComponent component, [NotNull] AssociatedData associatedData)
        {
            return associatedData.TransformComponent == entity.Transform
                && associatedData.CharacterComponent == entity.Get<CharacterComponent>()
                && associatedData.ClientPredictionSnapshotsComponent == entity.Get<ClientPredictionSnapshotsComponent>();
        }

        public void PreUpdate(GameTime gameTime)
        {
            if (!_gameClockManager.SimulationClock.IsNextSimulation)
            {
                // Only update during a simulation update
                return;
            }
            // Only host needs to create the snapshot data since it is the authoritative data,
            // the client will generate snapshots only when the host sends the data.
            if (_networkService.IsGameHost)
            {
                // Only generate new snapshot during a new simulation update
                var simTickNumber = _gameClockManager.SimulationClock.SimulationTickNumber;
                foreach (var kv in ComponentDatas)
                {
                    var movementSnapshotsComp = kv.Key;
                    var data = kv.Value;
                    CreateNewSnapshotData(simTickNumber, movementSnapshotsComp, data.TransformComponent);
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

            if (!_networkService.IsGameHost)
            {
                foreach (var kv in ComponentDatas)
                {
                    var data = kv.Value;
                    var clientPredictionSnapshotsComp = data.ClientPredictionSnapshotsComponent;
                    if (clientPredictionSnapshotsComp == null)
                    {
                        continue;
                    }
                    var predictedMovementsSpan = CollectionsMarshal.AsSpan(clientPredictionSnapshotsComp.PredictedMovements);
                    ref var predictedMovementData = ref predictedMovementsSpan[predictedMovementsSpan.Length - 1];      // Data was already created in MovementSnapshotsInputProcessor.Update, just need to save the position data
                    Debug.Assert(predictedMovementData.SimulationTickNumber == simTickNumber);

                    // Save resulting position in the predicted movement buffer
                    predictedMovementData.LocalPosition = data.TransformComponent.Position;
                    predictedMovementData.PhysicsEngineLinearVelocity = BulletPhysicsExt.GetLinearVelocity(data.CharacterComponent);
                    // Note LocalRotation is not obtained from any transform component, this is directly set on the movement data.

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
                    movementData.PhysicsEngineLinearVelocity = BulletPhysicsExt.GetLinearVelocity(data.CharacterComponent);
                    // Note LocalRotation is not obtained from any transform component, this is directly set on the movement data.
#if DEBUG
                    //Debug.WriteLine(@$"--FromSvr Pos: {movementData.LocalPosition} - Yaw: {movementData.YawOrientation} - Sim: {movementData.SimulationTickNumber} - PIDApplied {movementData.PlayerInputSequenceNumberApplied} - MvDir {movementData.MoveDirection} - IsGrounded {movementData.IsGrounded} - InpVel {movementData.CurrentMoveInputVelocity} - PhyVel {movementData.PhysicsEngineLinearVelocity}");
#endif
                }
            }
        }

        internal static ref MovementSnapshotsComponent.MovementData CreateNewSnapshotData(
            SimulationTickNumber simulationTickNumber,
            MovementSnapshotsComponent movementSnapshotsComponent,
            TransformComponent transformComponent)
        {
            var snapshotStore = movementSnapshotsComponent.SnapshotStore;
            var snapshotFindResult = snapshotStore.TryFindSnapshot(simulationTickNumber);
            if (snapshotFindResult.IsFound)
            {
                // Already exists, can happen when a new entity is created exactly on a new sim tick
                return ref snapshotFindResult.Result;
            }
            ref var movementData = ref snapshotStore.Create();
            var prevMovementFindResult = snapshotStore.TryFindSnapshot(simulationTickNumber - 1);
            if (prevMovementFindResult.IsFound)
            {
                // Just copy the previous snapshot's data
                movementData = prevMovementFindResult.Result;
            }
            else
            {
                // New data without any previous information
                MovementSnapshotsComponent.InitializeNewMovementData(ref movementData, transformComponent.Position, movementSnapshotsComponent.JumpReactionThreshold);
            }
            movementData.SimulationTickNumber = simulationTickNumber;
            return ref movementData;
        }

        internal class AssociatedData
        {
            internal TransformComponent TransformComponent;

            internal CharacterComponent CharacterComponent;
            internal ClientPredictionSnapshotsComponent ClientPredictionSnapshotsComponent;     // Optional
        }
    }
}
