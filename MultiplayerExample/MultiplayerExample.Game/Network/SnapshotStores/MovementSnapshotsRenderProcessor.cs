using MultiplayerExample.Core;
using MultiplayerExample.Engine;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using System;
using System.Diagnostics;

namespace MultiplayerExample.Network.SnapshotStores
{
    class MovementSnapshotsRenderProcessor : EntityProcessor<MovementSnapshotsComponent, MovementSnapshotsRenderProcessor.AssociatedData>,
        IInGameProcessor
    {
        private GameClockManager _gameClockManager;
        private GameEngineContext _gameEngineContext;

        public bool IsEnabled { get; set; }

        public MovementSnapshotsRenderProcessor() : base(typeof(TransformComponent), typeof(NetworkEntityComponent))
        {
            Order = -100000;         // Ensure this occurs before the mesh renderer processor to get the models in the right positions
            // Not using Enabled property, because that completely disables the processor, where it doesn't even pick up newly added entities
            IsEnabled = true;
        }

        protected override void OnSystemAdd()
        {
            _gameClockManager = Services.GetService<GameClockManager>();
            _gameEngineContext = Services.GetService<GameEngineContext>();
            Enabled = _gameEngineContext.IsClient;
        }

        protected override AssociatedData GenerateComponentData([NotNull] Entity entity, [NotNull] MovementSnapshotsComponent component)
        {
            return new AssociatedData
            {
                // Can also add other info/components here
                TransformComponent = entity.Transform,
                NetworkEntityComponent = entity.Get<NetworkEntityComponent>(),
                ModelChildTransform = entity.GetChild(0).Transform,

                ClientPredictionSnapshotsComponent = entity.Get<ClientPredictionSnapshotsComponent>(),
            };
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] MovementSnapshotsComponent component, [NotNull] AssociatedData data)
        {
            data.TransformComponent = null;
            data.ModelChildTransform = null;
            data.ClientPredictionSnapshotsComponent = null;
        }

        protected override bool IsAssociatedDataValid([NotNull] Entity entity, [NotNull] MovementSnapshotsComponent component, [NotNull] AssociatedData associatedData)
        {
            return associatedData.TransformComponent == entity.Transform
                && associatedData.NetworkEntityComponent == entity.Get<NetworkEntityComponent>()
                && associatedData.ModelChildTransform == entity.GetChild(0).Transform;
        }

#if DEBUG
        private Vector3 _lastRendPos;
#endif

        public override void Draw(RenderContext context)
        {
            if (!IsEnabled)
            {
                return;
            }

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
                var movementSnapshotsComp = kv.Key;
                var clientPredictionSnapshotsComp = data.ClientPredictionSnapshotsComponent;
                if (clientPredictionSnapshotsComp != null)
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
                    data.ModelChildTransform.Rotation = renderRot;
#if DEBUG
                    _lastRendPos = renderPos;
                    //DebugWriteLine(@$"Render SimFrom {fromMovementData.SimulationTickNumber} - SimTo {toMovementData.SimulationTickNumber} - Pos {renderPos} - Lerp {interpAmount} {fromMovementData.SnapshotType} {toMovementData.SnapshotType}
                    //OrigSimFrom {localFromSimTickNo} - OrigSimTo {localToSimTickNo} - Lerp {interpAmount} - TimeElapsed {_gameClockManager.SimulationClock.CurrentTickTimeElapsed}");
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
                    var networkComp = data.NetworkEntityComponent;

                    var fromSimTickNo = GetRenderFromSimulationTickNumber(networkComp.IsLocalEntity);
                    var toSimTickNo = GetRenderToSimulationTickNumber(networkComp.IsLocalEntity);
                    var simTickElapsedRatio = GetRenderSimulationTickElapsedRatio(networkComp.IsLocalEntity);

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
                        data.ModelChildTransform.Rotation = renderRot;

#if DEBUG
            //            if (!networkComp.IsLocalEntity)
            //            {
            //                //DebugWriteLine($"Render SimFrom {fromSimTickNo} - SimTo {toSimTickNo} - Id {networkComp.NetworkEntityId} - Pos {renderPos} - Lerp {simTickElapsedRatio}");
            //                DebugWriteLine(@$"Render SimFrom {fromMovementData.SimulationTickNumber} - SimTo {toMovementData.SimulationTickNumber} - Id {networkComp.NetworkEntityId} - Pos {renderPos} - Lerp {interpAmount} {fromMovementData.SnapshotType} {toMovementData.SnapshotType}
            //OrigSimFrom {fromSimTickNo} - OrigSimTo {toSimTickNo} - Lerp {simTickElapsedRatio} - TimeElapsed {_gameClockManager.SimulationClock.CurrentTickTimeElapsed}");
            //            }
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
            internal NetworkEntityComponent NetworkEntityComponent;
            internal TransformComponent ModelChildTransform;

            internal ClientPredictionSnapshotsComponent ClientPredictionSnapshotsComponent;   // Optional
        }
    }
}
