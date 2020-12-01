using MultiplayerExample.Core;
using MultiplayerExample.GameServices;
using MultiplayerExample.Network.EntityMessages;
using MultiplayerExample.Network.NetworkMessages;
using MultiplayerExample.Network.NetworkMessages.Client;
using MultiplayerExample.Network.NetworkMessages.Server;
using MultiplayerExample.Network.SnapshotStores;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using System;
using System.Diagnostics;

namespace MultiplayerExample.Network
{
    partial class NetworkEntityProcessor
    {
        /// <summary>
        /// Manager for all players when this game is a client.
        /// This is just a lightweight container to contain all client related functions.
        /// </summary>
        internal class ClientPlayerManager
        {
            private readonly NetworkEntityProcessor _networkEntityProcessor;
            private readonly FastList<LocalPlayerDetails> _localPlayers;
            // Working list to hold entities to pass to MovementSnapshotsInputProcessor
            private readonly FastList<MovementSnapshotsInputProcessor.PredictMovementEntityData> _resimulateEntities;

            public ClientPlayerManager(NetworkEntityProcessor networkEntityProcessor)
            {
                _networkEntityProcessor = networkEntityProcessor;

                _localPlayers = new FastList<LocalPlayerDetails>();
                _resimulateEntities = new FastList<MovementSnapshotsInputProcessor.PredictMovementEntityData>();
            }

            internal void SendClientInputPredictionsToServer(SimulationTickNumber currentSimTickNumber, NetworkMessageWriter networkMessageWriter)
            {
                for (int playerIdx = 0; playerIdx < _localPlayers.Count; playerIdx++)
                {
                    ref var player = ref _localPlayers.Items[playerIdx];
                    var inputSnapshotsComp = player.InputSnapshotsComponent;

                    PlayerUpdateMessage playerUpdateMsg = default;

                    var pendingInputs = inputSnapshotsComp.PendingInputs;
                    // Need to limit the number of input, eg. if there's a large delay in server ack response,
                    // then most likely it has lost some of our older input. Probably should be a config setting?
                    const int MaxPendingPlayerInputs = GameConfig.PhysicsSimulationRate * 20;    // 20s worth of input
                    const int MaxSendPlayerInputs = 10;     // We will only send 10 inputs max in a single packet, and accept the fact that older inputs may be lost
                    if (pendingInputs.Count >= MaxPendingPlayerInputs)
                    {
                        pendingInputs.RemoveRange(0, pendingInputs.Count - MaxPendingPlayerInputs + 1);
                    }
                    // Append the latest input
                    var findSnapshotResult = inputSnapshotsComp.SnapshotStore.TryFindSnapshot(currentSimTickNumber);
                    Debug.Assert(findSnapshotResult.IsFound);
                    pendingInputs.Add(findSnapshotResult.Result);

                    int sendInputCount = Math.Min(pendingInputs.Count, MaxSendPlayerInputs);
                    // At most, only send the last 10 inputs (ie. the current inputs) to not blow out the packet size.
                    int inputIndexOffset = pendingInputs.Count - sendInputCount;
                    for (int i = 0; i < sendInputCount; i++)
                    {
                        int inputIndex = i + inputIndexOffset;
                        if (pendingInputs[inputIndex].PlayerInputSequenceNumber > inputSnapshotsComp.ServerLastAcknowledgedPlayerInputSequenceNumber)
                        {
                            inputIndexOffset += i;
                            sendInputCount -= i;
                            break;
                        }
                    }

                    networkMessageWriter.Reset();
                    playerUpdateMsg.AcknowledgedServerSimulationTickNumber = player.NetworkEntityComponent.LastAcknowledgedServerSimulationTickNumber;
                    playerUpdateMsg.WriteTo(networkMessageWriter);
                    PlayerUpdateInputMessage playerUpdateInputsMsg = default;
                    playerUpdateInputsMsg.WriteHeader(networkMessageWriter, (ushort)sendInputCount);

                    for (int i = 0; i < sendInputCount; i++)
                    {
                        int inputIndex = i + inputIndexOffset;
                        ref var curInputData = ref pendingInputs.Items[inputIndex];
                        playerUpdateInputsMsg.PlayerInputSequenceNumber = curInputData.PlayerInputSequenceNumber;
                        playerUpdateInputsMsg.MoveInput = curInputData.MoveInput;
                        playerUpdateInputsMsg.JumpRequestedInput = curInputData.IsJumpButtonDown;

                        playerUpdateInputsMsg.WriteNextArrayItem(networkMessageWriter);
#if DEBUG
                        //if (curInputData.MoveInput.LengthSquared() > 0)
                        //{
                        //    _networkEntityProcessor.DebugWriteLine($"Cln SendInput Move: {curInputData.MoveInput} - PISeqNo: {curInputData.PlayerInputSequenceNumber}");
                        //}
#endif
                    }
                    var connection = player.Connection;
                    connection.Send(networkMessageWriter, SendNetworkMessageType.Unreliable);
                    //_networkEntityProcessor.DebugWriteLine($"Cln SendInput {inputCount}");
                }
            }

            internal bool AcknowledgeReceivedPlayerInputs(
                PlayerInputSequenceNumber lastAcknowledgedPlayerInputSequenceNumber,
                PlayerInputSequenceNumber lastAppliedPlayerInputSequenceNumber)
            {
                bool hasServerAppliedNewPlayerInput = false;
                for (int playerIdx = 0; playerIdx < _localPlayers.Count; playerIdx++)
                {
                    ref var player = ref _localPlayers.Items[playerIdx];
                    var inputSnapshotsComp = player.InputSnapshotsComponent;
                    if (inputSnapshotsComp.ServerLastAcknowledgedPlayerInputSequenceNumber < lastAcknowledgedPlayerInputSequenceNumber)
                    {
                        inputSnapshotsComp.ServerLastAcknowledgedPlayerInputSequenceNumber = lastAcknowledgedPlayerInputSequenceNumber;
                    }
                    if (inputSnapshotsComp.ServerLastAppliedPlayerInputSequenceNumber < lastAppliedPlayerInputSequenceNumber)
                    {
                        inputSnapshotsComp.ServerLastAppliedPlayerInputSequenceNumber = lastAppliedPlayerInputSequenceNumber;
                        hasServerAppliedNewPlayerInput = true;

                        var pendingInputs = inputSnapshotsComp.PendingInputs;
                        int removeCount = 0;
                        for (int i = 0; i < pendingInputs.Count; i++)
                        {
                            if (pendingInputs.Items[i].PlayerInputSequenceNumber <= lastAppliedPlayerInputSequenceNumber)
                            {
                                removeCount++;
                            }
                            else
                            {
                                break;
                            }
                        }
                        pendingInputs.RemoveRange(0, removeCount);
                    }
                }
                return hasServerAppliedNewPlayerInput;
            }

            internal void UpdateEntityStates(
                SimulationTickNumber currentSimulationTickNumber,
                bool hasServerAppliedNewPlayerInput,
                FastList<EntityUpdateTransform> updateEntityTransforms,
                FastList<EntityUpdateInputAction> updateEntityInputs,
                PlayerInputSequenceNumber lastAppliedServerPlayerInputSequenceNumber)
            {
                var networkEntityIdToEntityDataMap = _networkEntityProcessor._networkEntityIdToEntityDataMap;
                for (int i = 0; i < updateEntityTransforms.Count; i++)
                {
                    ref var updateTransform = ref updateEntityTransforms.Items[i];
                    if (!networkEntityIdToEntityDataMap.TryGetValue(updateTransform.NetworkEntityId, out var data))
                    {
                        // Since network messages can occur out of order, the entity might already have been removed
                        continue;
                    }
                    var networkEntityComp = data.NetworkEntityComponent;
                    var svrSimTickNumber = updateTransform.SimulationTickNumber;
                    if (networkEntityComp.LastAcknowledgedServerSimulationTickNumber >= svrSimTickNumber)
                    {
                        continue;   // Redundant message, already added
                    }
                    var movementSnapshotsComp = data.MovementSnapshotsComponent;
                    Debug.Assert(movementSnapshotsComp != null);

                    ref var movementData = ref movementSnapshotsComp.SnapshotStore.GetOrCreate(svrSimTickNumber);

                    movementData.SimulationTickNumber = svrSimTickNumber;
                    movementData.LocalPosition = updateTransform.Position;
                    movementData.SetRotationFromYawOrientation(updateTransform.YawOrientation);
                    movementData.MoveSpeedDecimalPercentage = updateTransform.MoveSpeedDecimalPercentage;
                    movementData.CurrentMoveInputVelocity = updateTransform.CurrentMoveInputVelocity;
                    movementData.PhysicsEngineLinearVelocity = updateTransform.PhysicsEngineLinearVelocity;
                    if (movementSnapshotsComp.MaxRunSpeed != 0)
                    {
                        // This is reverse calculation seen in MovementSnapshotsInputProcessor.Move
                        movementData.MoveDirection = movementData.CurrentMoveInputVelocity / movementSnapshotsComp.MaxRunSpeed;
                    }
                    else
                    {
                        movementData.MoveDirection = Vector3.Zero;
                    }
                    movementData.IsGrounded = updateTransform.IsGrounded;
                    movementData.PlayerInputSequenceNumberApplied = hasServerAppliedNewPlayerInput ? lastAppliedServerPlayerInputSequenceNumber : default;     // Not completely accurate, but good enough for debugging purposes...

#if DEBUG
                    //if (networkEntityComp.IsLocalEntity)// && movementData.MoveSpeedDecimalPercentage == 0)
                    //{
                    //    _networkEntityProcessor.DebugWriteLine($"UpdateEnt Sim {svrSimTickNumber} - Id {updateTransform.NetworkEntityId} - PIDApplied {movementData.PlayerInputSequenceNumberApplied} - Pos {movementData.LocalPosition} -  MvDir {movementData.MoveDirection} - IsGrounded {movementData.IsGrounded} - Vel {movementData.PhysicsEngineLinearVelocity} - SpdPec {movementData.MoveSpeedDecimalPercentage}");
                    //}
                    //if (networkEntityComp.IsLocalEntity && movementData.LocalPosition.X != -5)
                    //{
                    //    _networkEntityProcessor.DebugWriteLine($"ClnUpdateEnt Sim {svrSimTickNumber} - Id {updateTransform.NetworkEntityId} - Pos {movementData.LocalPosition}");
                    //    int printCount = Math.Min(movementSnapshotsComp.SnapshotStore.Count, 15) - 1;
                    //    for (int jj = printCount; jj >= 0; jj--)
                    //    {
                    //        ref var mm = ref movementSnapshotsComp.SnapshotStore.GetPrevious(jj);
                    //        _networkEntityProcessor.DebugWriteLine($"ClnMvm Sim {mm.SimulationTickNumber} - Pos {mm.LocalPosition} - PIDApplied {mm.PlayerInputSequenceNumberApplied} - MvDir {mm.MoveDirection}");
                    //    }
                    //}
#endif
                    networkEntityComp.LastAcknowledgedServerSimulationTickNumber = svrSimTickNumber;

                    var clientPredictionSnapshotsComp = data.ClientPredictionSnapshotsComponent;
                    if (networkEntityComp.IsLocalEntity && clientPredictionSnapshotsComp != null)
                    {
                        bool exists = false;
                        for (int j = 0; j < _resimulateEntities.Count; j++)
                        {
                            if (_resimulateEntities.Items[j].TransformComponent == data.TransformComponent)
                            {
                                exists = true;
                                break;
                            }
                        }
                        if (!exists)
                        {
                            bool shouldResimulate = true;
                            if (hasServerAppliedNewPlayerInput)
                            {
                                var predictedMovements = clientPredictionSnapshotsComp.PredictedMovements;
                                for (int mvmtIndx = 0; mvmtIndx < predictedMovements.Count; mvmtIndx++)
                                {
                                    ref var predictedMovementData = ref predictedMovements.Items[mvmtIndx];
                                    if (predictedMovementData.PlayerInputSequenceNumberApplied == lastAppliedServerPlayerInputSequenceNumber)
                                    {
                                        var posDiff = predictedMovementData.LocalPosition - updateTransform.Position;
                                        if (posDiff.LengthSquared() < 0.00001f)
                                        {
                                            shouldResimulate = false;
                                            int removeCount = mvmtIndx + 1 - 2;
                                            if (removeCount > 0)
                                            {
                                                // This will shift the predictions down so the last confirmed position
                                                // is at index 1 (ie. the first two values in the predicted list are actually
                                                // confirmed positions to ensure we can do render interpolation)
                                                predictedMovements.RemoveRange(0, removeCount);
                                            }
                                            //Debug.WriteLine($"No resimulation required. Removed {removeCount} predictions.");
                                        }
                                        break;
                                    }
                                }
                            }
                            if (shouldResimulate)
                            {
                                //Debug.WriteLine($"Resimulation required.");
                                _resimulateEntities.Add(new MovementSnapshotsInputProcessor.PredictMovementEntityData(
                                    data.TransformComponent,
                                    data.Entity.Get<Stride.Physics.CharacterComponent>(),
                                    data.Entity.Get<InputSnapshotsComponent>(),
                                    movementSnapshotsComp,
                                    clientPredictionSnapshotsComp
                                ));
                            }
                        }
                    }
                    else if (!networkEntityComp.IsLocalEntity && clientPredictionSnapshotsComp == null)
                    {
                        var characterComp = data.CharacterComponent;
                        if (characterComp != null)
                        {
                            var transformComp = data.TransformComponent;
                            transformComp.Position = movementData.LocalPosition;
                            //transformComp.Rotation isn't set on the remote entity (it's only set on the view entity's model entity)
                            characterComp.UpdatePhysicsTransformation();
                        }
                    }
                }

                for (int i = 0; i < updateEntityInputs.Count; i++)
                {
                    ref var updateInput = ref updateEntityInputs.Items[i];
                    Debug.Assert(networkEntityIdToEntityDataMap.ContainsKey(updateInput.NetworkEntityId));
                    var data = networkEntityIdToEntityDataMap[updateInput.NetworkEntityId];
                    var networkEntityComp = data.NetworkEntityComponent;
                    if (networkEntityComp.LastAcknowledgedServerSimulationTickNumber >= updateInput.SimulationTickNumber)
                    {
                        continue;   // Redundant message, already added
                    }
                    switch (updateInput.InputActionType)
                    {
                        case InputActionType.Jump:
                            // TODO: ??
                            break;
                        case InputActionType.Melee:
                            // TODO: ??
                            break;
                        case InputActionType.Shoot:
                            // TODO: ??
                            break;
                    }
                }

                if (_resimulateEntities.Count > 0)
                {
                    var movementSnapshotsInputProcessor = _networkEntityProcessor._lazyLoadedScene.GetMovementSnapshotsInputProcessor();
                    movementSnapshotsInputProcessor.Resimulate(currentSimulationTickNumber, _resimulateEntities);
                    _resimulateEntities.Clear();
                }
            }

            internal void SpawnLocalPlayer(SpawnLocalPlayerMessage spawnPlayerMessage, NetworkConnection connectionToServer)
            {
                var simTickNumber = spawnPlayerMessage.SimulationTickNumber;
                var playerEntity = _networkEntityProcessor.CreateAndAddClientPlayerEntity(simTickNumber, spawnPlayerMessage.PlayerId, spawnPlayerMessage.PlayerName, ref spawnPlayerMessage.Position, ref spawnPlayerMessage.Rotation, isLocalEntity: true);

                var localPlayer = new LocalPlayerDetails(spawnPlayerMessage.PlayerId, playerEntity, connectionToServer);
                Debug.Assert(!_localPlayers.Exists(x => x.PlayerId == spawnPlayerMessage.PlayerId));
                _localPlayers.Add(localPlayer);

                var gameManager = GetGameManager();
                gameManager.RaisePlayerAddedEvent(playerEntity);
            }

            internal void DespawnAllLocalPlayers()
            {
                var gameplayScene = GetGameplayScene();
                var gameManager = GetGameManager();
                for (int i = 0; i < _localPlayers.Count; i++)
                {
                    ref var player = ref _localPlayers.Items[i];
                    var playerEntity = player.NetworkEntityComponent.Entity;
                    _networkEntityProcessor.RemoveAndUnregisterEntity(player.PlayerId, playerEntity, gameplayScene);
                    gameManager.RaisePlayerRemovedEntity(playerEntity);
                }
                _localPlayers.Clear();
            }

            internal void SpawnRemotePlayer(SpawnRemotePlayerMessage spawnPlayerMessage)
            {
                var simTickNumber = spawnPlayerMessage.SimulationTickNumber;
                var playerEntity = _networkEntityProcessor.CreateAndAddClientPlayerEntity(simTickNumber, spawnPlayerMessage.PlayerId, spawnPlayerMessage.PlayerName, ref spawnPlayerMessage.Position, ref spawnPlayerMessage.Rotation, isLocalEntity: false);

                var gameManager = GetGameManager();
                gameManager.RaisePlayerAddedEvent(playerEntity);
            }

            //internal void UnregisterLocalPlayer(SerializableGuid playerId)
            //{
            //    Debug.Assert(_localPlayers.Exists(x => x.PlayerId == playerId));
            //    _localPlayers.RemoveAll(x => x.PlayerId == playerId);
            //}

            internal void DespawnRemotePlayer(DespawnRemotePlayerMessage despawnPlayerMessage)
            {
                var gameplayScene = GetGameplayScene();

                var networkEntityIdToEntityDataMap = _networkEntityProcessor._networkEntityIdToEntityDataMap;
                if (networkEntityIdToEntityDataMap.TryGetValue(despawnPlayerMessage.PlayerId, out var data))
                {
                    var entity = data.Entity;
                    _networkEntityProcessor.RemoveAndUnregisterEntity(despawnPlayerMessage.PlayerId, entity, gameplayScene);
                    var gameManager = GetGameManager();
                    gameManager.RaisePlayerRemovedEntity(entity);
                }
                else
                {
                    Debug.Fail($"Remote Player was not spawned: {despawnPlayerMessage.PlayerId}");
                }
            }

            private GameManager GetGameManager() => _networkEntityProcessor._lazyLoadedScene.GetGameManager();
            private Scene GetGameplayScene() => _networkEntityProcessor._lazyLoadedScene.GetGameplayScene();

            private readonly struct LocalPlayerDetails
            {
                /// <summary>
                /// This is the same as the NetworkEntityId.
                /// </summary>
                public readonly SerializableGuid PlayerId;
                public readonly NetworkEntityComponent NetworkEntityComponent;
                public readonly InputSnapshotsComponent InputSnapshotsComponent;
                public readonly MovementSnapshotsComponent MovementSnapshotsComponent;
                /// <summary>
                /// Connection to the server.
                /// </summary>
                public readonly NetworkConnection Connection;

                public LocalPlayerDetails(SerializableGuid playerId, Entity playerEntity, NetworkConnection connection)
                {
                    PlayerId = playerId;
                    NetworkEntityComponent = playerEntity.Get<NetworkEntityComponent>();
                    InputSnapshotsComponent = playerEntity.Get<InputSnapshotsComponent>();
                    Debug.Assert(InputSnapshotsComponent != null, $"{InputSnapshotsComponent} must exist.");
                    MovementSnapshotsComponent = playerEntity.Get<MovementSnapshotsComponent>();
                    Connection = connection;
                }
            }
        }
    }
}
