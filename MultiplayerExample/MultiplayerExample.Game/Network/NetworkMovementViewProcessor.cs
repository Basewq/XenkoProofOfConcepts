using MultiplayerExample.Core;
using MultiplayerExample.Engine;
using MultiplayerExample.Network.SnapshotStores;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using System;
using System.Diagnostics;

namespace MultiplayerExample.Network
{
    class NetworkMovementViewProcessor : EntityProcessor<NetworkEntityViewComponent, NetworkMovementViewProcessor.AssociatedData>
    {
        private GameClockManager _gameClockManager;
        private IGameNetworkService _networkService;

        public NetworkMovementViewProcessor()
        {
            Order = -100000;         // Ensure this occurs before the mesh renderer processor to get the models in the right positions
        }

        protected override void OnSystemAdd()
        {
            var gameEngineContext = Services.GetService<GameEngineContext>();
            Enabled = gameEngineContext.IsClient;

            _gameClockManager = Services.GetService<GameClockManager>();
            _networkService = Services.GetService<IGameNetworkService>();
        }

        protected override void OnSystemRemove()
        {
        }

        protected override AssociatedData GenerateComponentData([NotNull] Entity entity, [NotNull] NetworkEntityViewComponent component)
        {
            return new AssociatedData
            {
                // Can also add other info/components here
                TransformComponent = entity.Transform,
                ModelChildTransformComponent = entity.GetChild(0).Transform
            };
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] NetworkEntityViewComponent component, [NotNull] AssociatedData data)
        {
            var networkedEntity = component.NetworkedEntity;
            Debug.Assert(networkedEntity != null, $"{nameof(NetworkEntityViewComponent)} must reference another entity.");
            Debug.Assert(networkedEntity != entity, $"{nameof(NetworkEntityViewComponent)} cannot reference itself.");

            // Networked entity's components - Assume these never get reassigned,
            data.NetworkEntityComponent = networkedEntity.Get<NetworkEntityComponent>();
            data.MovementSnapshotsComponent = networkedEntity.Get<MovementSnapshotsComponent>();
            data.ClientPredictionSnapshotsComponent = networkedEntity.Get<ClientPredictionSnapshotsComponent>();
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] NetworkEntityViewComponent component, [NotNull] AssociatedData data)
        {
            data.TransformComponent = null;
            data.ModelChildTransformComponent = null;

            data.NetworkEntityComponent = null;
            data.MovementSnapshotsComponent = null;
            data.ClientPredictionSnapshotsComponent = null;
        }

        protected override bool IsAssociatedDataValid([NotNull] Entity entity, [NotNull] NetworkEntityViewComponent component, [NotNull] AssociatedData associatedData)
        {
            return associatedData.TransformComponent == entity.Transform
                && associatedData.ModelChildTransformComponent == entity.GetChild(0).Transform;
        }

        public override void Draw(RenderContext context)
        {
            var localFromSimTickNo = _gameClockManager.SimulationClock.SimulationTickNumber - 1;
            var localToSimTickNo = localFromSimTickNo + 1;
            var localSimTickElapsedRatio = (float)(_gameClockManager.SimulationClock.CurrentTickTimeElapsed.TotalMilliseconds / GameConfig.PhysicsFixedTimeStep.TotalMilliseconds);

            var renderRemoteDelayTime = _gameClockManager.RemoteEntityRenderTimeDelay + GameConfig.PhysicsFixedTimeStep;    // Always add one snapshot delay so we're at least interpolating from the previous snapshot to the current snaphot
            var renderRemoteWorldTime = _gameClockManager.SimulationClock.TotalTime - renderRemoteDelayTime;
            if (renderRemoteWorldTime < TimeSpan.Zero)
            {
                renderRemoteWorldTime = TimeSpan.Zero;
            }

            var remoteFromSimTickNo = GameClockManager.CalculateSimulationTickNumber(renderRemoteWorldTime);
            var remoteToSimTickNo = remoteFromSimTickNo + 1;
            var remoteSimTickElapsed = TimeSpan.FromTicks(renderRemoteWorldTime.Ticks - (GameConfig.PhysicsFixedTimeStep.Ticks * remoteFromSimTickNo));
            var remoteSimTickElapsedRatio = (float)(remoteSimTickElapsed.TotalMilliseconds / GameConfig.PhysicsFixedTimeStep.TotalMilliseconds);

            foreach (var kv in ComponentDatas)
            {
                var data = kv.Value;
                var movementSnapshotsComp = data.MovementSnapshotsComponent;
                var clientPredictionSnapshotsComp = data.ClientPredictionSnapshotsComponent;
                var networkEntityComp = data.NetworkEntityComponent;
                if (networkEntityComp.IsLocalEntity && clientPredictionSnapshotsComp != null && !_networkService.IsGameHost)
                {
                    var predictedMovements = clientPredictionSnapshotsComp.PredictedMovements;
#if DEBUG
                    //DebugWriteLine(@$"RENDER -- SimTick {_gameClockManager.SimulationClock.SimulationTickNumber} - SimTotalTime {_gameClockManager.SimulationClock.TotalTime} - SimCurElapsed {_gameClockManager.SimulationClock.CurrentTickTimeElapsed} - Lerp {localSimTickElapsedRatio}");
#endif
                    if (predictedMovements.Count < 2)
                    {
                        // Not enough positions
                        continue;
                    }

                    // Always just use the last two data points
                    ref var fromMovementData = ref predictedMovements.Items[predictedMovements.Count - 2];
                    ref var toMovementData = ref predictedMovements.Items[predictedMovements.Count - 1];

                    var interpAmount = localSimTickElapsedRatio;
                    Vector3.Lerp(ref fromMovementData.LocalPosition, ref toMovementData.LocalPosition, interpAmount, out var renderPos);
                    Quaternion.Slerp(ref fromMovementData.LocalRotation, ref toMovementData.LocalRotation, interpAmount, out var renderRot);
                    data.TransformComponent.Position = renderPos;
                    data.ModelChildTransformComponent.Rotation = renderRot;
#if DEBUG
//                    DebugWriteLine(@$"Render PIDFrom {fromMovementData.PlayerInputSequenceNumberApplied} - PIDTo {toMovementData.PlayerInputSequenceNumberApplied} - RndPos {renderPos} - RndRot {renderRot} - FromRot {fromMovementData.LocalRotation} - ToRot {toMovementData.LocalRotation}
//                    OrigSimFrom {localFromSimTickNo} - OrigSimTo {localToSimTickNo} - Lerp {interpAmount} - TimeElapsed {_gameClockManager.SimulationClock.CurrentTickTimeElapsed}");
#endif
                }
                else
                {
                    var snapshotStore = movementSnapshotsComp.SnapshotStore;
                    if (snapshotStore.Count < 2)
                    {
                        // Not enough snapshots
                        continue;
                    }

                    var fromSimTickNo = GetRenderFromSimulationTickNumber(networkEntityComp.IsLocalEntity);
                    var toSimTickNo = GetRenderToSimulationTickNumber(networkEntityComp.IsLocalEntity);
                    var simTickElapsedRatio = GetRenderSimulationTickElapsedRatio(networkEntityComp.IsLocalEntity);

                    var fromFindResult = snapshotStore.TryFindSnapshotClosestEqualOrLessThan(fromSimTickNo);
                    if (!fromFindResult.IsFound)
                    {
                        // No starting point to interpolate from
                        continue;
                    }

                    var toFindResult = snapshotStore.TryFindSnapshotClosestEqualOrGreaterThan(toSimTickNo);
                    if (!toFindResult.IsFound)
                    {
                        toFindResult = fromFindResult;      // No more snapshots found, just use the last snapshot
                        toSimTickNo = fromSimTickNo;
                    }

#if DEBUG
                    //if (networkComp.IsLocalEntity)
                    //{
                    //    DebugWriteLine($"Render SimFrom {fromSimTickNo} - SimTo {toSimTickNo} - Id {networkComp.NetworkEntityId} - Pos {fromFindResult.Result.LocalPosition}");
                    //}
#endif
                    {

                        ref var fromMovementData = ref fromFindResult.Result;
                        ref var toMovementData = ref toFindResult.Result;

                        var interpAmount = GetRenderInterpolationAmount(fromSimTickNo, simTickElapsedRatio, fromMovementData.SimulationTickNumber, toMovementData.SimulationTickNumber);
                        Vector3.Lerp(ref fromMovementData.LocalPosition, ref toMovementData.LocalPosition, interpAmount, out var renderPos);
                        Quaternion.Slerp(ref fromMovementData.LocalRotation, ref toMovementData.LocalRotation, interpAmount, out var renderRot);
                        data.TransformComponent.Position = renderPos;
                        data.ModelChildTransformComponent.Rotation = renderRot;

#if DEBUG
//                        if (!networkComp.IsLocalEntity)
//                        {
//                            //DebugWriteLine($"Render SimFrom {fromSimTickNo} - SimTo {toSimTickNo} - Id {networkComp.NetworkEntityId} - Pos {renderPos} - Lerp {simTickElapsedRatio}");
//                            DebugWriteLine(@$"Render SimFrom {fromMovementData.SimulationTickNumber} - SimTo {toMovementData.SimulationTickNumber} - Id {networkComp.NetworkEntityId} - Pos {renderPos}
//            OrigSimFrom {fromSimTickNo} - OrigSimTo {toSimTickNo} - Lerp {simTickElapsedRatio} - TimeElapsed {_gameClockManager.SimulationClock.CurrentTickTimeElapsed}");
//                        }
#endif
                    }
                }
            }

            SimulationTickNumber GetRenderFromSimulationTickNumber(bool isLocalEntity)
            {
                return isLocalEntity ? localFromSimTickNo : remoteFromSimTickNo;
            }

            SimulationTickNumber GetRenderToSimulationTickNumber(bool isLocalEntity)
            {
                return isLocalEntity ? localToSimTickNo : remoteToSimTickNo;
            }

            float GetRenderSimulationTickElapsedRatio(bool isLocalEntity)
            {
                return isLocalEntity ? localSimTickElapsedRatio : remoteSimTickElapsedRatio;
            }

            static float GetRenderInterpolationAmount(SimulationTickNumber expectedFromSimTickNo, float simTickInterpolationAmount, SimulationTickNumber renderFromSimTickNo, SimulationTickNumber renderToSimTickNo)
            {
                // Note: don't need 'expectedToSimTickNo' because this is just (expectedFromSimTickNo + 1)
                var renderSimTickDelta = renderToSimTickNo - renderFromSimTickNo;
                if (renderSimTickDelta == 0)
                {
                    return 0;
                }

                float renderFromSimOffset = (expectedFromSimTickNo - renderFromSimTickNo) + simTickInterpolationAmount;   // Equivalent to (expectedFromSimTickNo + simTickInterpolationAmount) - renderFromSimTickNo
                var interpAmount = renderFromSimOffset / renderSimTickDelta;
                return MathUtil.Clamp(interpAmount, 0, 1);
            }
        }

        [Conditional("DEBUG")]
        private void DebugWriteLine(string message)
        {
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }

        internal class AssociatedData
        {
            internal TransformComponent TransformComponent;
            internal TransformComponent ModelChildTransformComponent;

            // Components on the Networked entity
            internal NetworkEntityComponent NetworkEntityComponent;
            internal MovementSnapshotsComponent MovementSnapshotsComponent;
            internal ClientPredictionSnapshotsComponent ClientPredictionSnapshotsComponent;   // Optional component
        }
    }
}
