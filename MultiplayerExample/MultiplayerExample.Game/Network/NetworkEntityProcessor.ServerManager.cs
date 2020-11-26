using MultiplayerExample.Core;
using MultiplayerExample.Network.EntityMessages;
using MultiplayerExample.Network.NetworkMessages;
using MultiplayerExample.Network.NetworkMessages.Client;
using MultiplayerExample.Network.NetworkMessages.Server;
using MultiplayerExample.Network.SnapshotStores;
using MultiplayerExample.Utilities;
using Stride.Core.Collections;
using Stride.Core.Serialization;
using Stride.Engine;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MultiplayerExample.Network
{
    partial class NetworkEntityProcessor
    {
        internal void Server_AddPlayer(SerializableGuid playerId, string playerName, NetworkConnection connection)
        {
            var player = new ServerPendingPlayer(playerId, playerName, connection);
            _serverPlayerManager.AddPendingPlayer(player);
        }

        internal void Server_RemovePlayer(SerializableGuid playerId)
        {
            _serverPlayerManager.RemovePlayer(playerId, _networkMessageWriter);
        }

        internal void Server_SetPlayerReady(SerializableGuid playerId)
        {
            _serverPlayerManager.SetPlayerReady(playerId);
        }

        internal void Server_CollectPendingInputs(
            SerializableGuid playerId,
            PlayerUpdateMessage pendingPlayerUpdateMessage,
            FastList<PlayerUpdateInputMessage> pendingPlayerUpdateInputsMessages)
        {
            _serverPlayerManager.CollectPendingInputs(playerId, pendingPlayerUpdateMessage, pendingPlayerUpdateInputsMessages);
        }

        internal void Server_SendSynchronizeClockResponse(SerializableGuid playerId, SynchronizeClockRequestMessage syncClockRequestMsg)
        {
            _networkMessageWriter.Reset();

            var syncClockResponse = new SynchronizeClockResponseMessage
            {
                ClientOSTimeStamp = syncClockRequestMsg.ClientOSTimestamp,
                ServerWorldTimeInTicks = _gameClockManager.SimulationClock.TotalTime.Ticks,
            };

            NetworkConnection? conn = null;
            //lock (_serverPlayerManager.PendingPlayers)
            {
                int clientIndex = _serverPlayerManager.PendingPlayers.FindIndex(x => x.PlayerId == playerId);
                if (clientIndex >= 0)
                {
                    ref var player = ref _serverPlayerManager.PendingPlayers.Items[clientIndex];
                    conn = player.Connection;
                }
            }
            if (!conn.HasValue)
            {
                int clientIndex = _serverPlayerManager.ActivePlayers.FindIndex(x => x.PlayerId == playerId);
                if (clientIndex >= 0)
                {
                    ref var player = ref _serverPlayerManager.ActivePlayers.Items[clientIndex];
                    conn = player.Connection;
                }
            }
            Debug.Assert(conn.HasValue);

            syncClockResponse.WriteTo(_networkMessageWriter);
            conn.Value.Send(_networkMessageWriter, SendNetworkMessageType.Unreliable);
        }

        /// <summary>
        /// Manager for all players when this game is being hosted or is a dedicated server.
        /// This is just a lightweight container to contain all server related functions.
        /// </summary>
        private struct ServerPlayerManager
        {
            private readonly NetworkEntityProcessor _networkEntityProcessor;

            private readonly List<EntityExistenceDetails> _workingUpdateEntities;
            private readonly List<EntityUpdateInputAction> _workingUpdateEntityInputs;

            public readonly FastList<ServerPendingPlayer> PendingPlayers;
            public readonly FastList<ServerActivePlayer> ActivePlayers;

            public ServerPlayerManager(int initialCapacity, NetworkEntityProcessor networkEntityProcessor)
            {
                PendingPlayers = new FastList<ServerPendingPlayer>(initialCapacity);
                ActivePlayers = new FastList<ServerActivePlayer>(initialCapacity);

                _networkEntityProcessor = networkEntityProcessor;

                _workingUpdateEntities = new List<EntityExistenceDetails>();
                _workingUpdateEntityInputs = new List<EntityUpdateInputAction>();
            }

            internal void UpdatePendingPlayerStates(NetworkMessageWriter networkMessageWriter)
            {
                var networkAssetDatabase = _networkEntityProcessor._networkAssetDatabase;
                var assetDefinitions = _networkEntityProcessor._lazyLoadedScene.GetNetworkAssetDefinitions();

                //lock (PendingPlayers)
                for (int i = 0; i < PendingPlayers.Count; i++)
                {
                    ref var pendingPlayer = ref PendingPlayers.Items[i];
                    switch (pendingPlayer.PendingPlayerState)
                    {
                        case PendingPlayerState.JustConnected:
                            {
                                pendingPlayer.PendingPlayerState = PendingPlayerState.LoadingScene;

                                networkMessageWriter.Reset();
                                var joinGameResponseMsg = new ClientJoinGameResponseMessage
                                {
                                    CanJoinGame = true,
                                    InGameSceneAssetId = networkAssetDatabase.GetAssetIdFromUrlReference(assetDefinitions.InGameScene),
                                };
                                joinGameResponseMsg.WriteTo(networkMessageWriter);
                                pendingPlayer.Connection.Send(networkMessageWriter, SendNetworkMessageType.ReliableOrdered);
                            }
                            break;
                        case PendingPlayerState.LoadingScene:
                            // Keep waiting for the player to load the scene
                            // TODO: maybe do a timeout and/or ping check?
                            break;
                        case PendingPlayerState.Ready:
                            OnNewPlayerReady(pendingPlayer, networkMessageWriter);
                            break;
                        default:
                            break;
                    }
                }
                PendingPlayers.RemoveAll(x => x.PendingPlayerState == PendingPlayerState.Ready);
            }

            private void OnNewPlayerReady(ServerPendingPlayer pendingPlayer, NetworkMessageWriter networkMessageWriter)
            {
                var networkAssetDatabase = _networkEntityProcessor._networkAssetDatabase;
                var assetDefinitions = _networkEntityProcessor._lazyLoadedScene.GetNetworkAssetDefinitions();
                var assetDatabase = _networkEntityProcessor._networkAssetDatabase;
                var content = _networkEntityProcessor._content;
                var gameClockManager = _networkEntityProcessor._gameClockManager;

                var entityExistenceStates = _networkEntityProcessor._entityExistenceStates;

                var gameplayScene = _networkEntityProcessor._lazyLoadedScene.GetGameplayScene();

                var simTickNumber = gameClockManager.SimulationClock.SimulationTickNumber;
                // Can add to the scene now
                var prefabUrl = assetDefinitions.PlayerAssets.ServerRemotePlayer;
                var playerPrefab = content.Load(prefabUrl);
                var playerEntity = playerPrefab.InstantiateSingle();
                var networkEntityComp = playerEntity.Get<NetworkEntityComponent>();
                networkEntityComp.NetworkEntityId = pendingPlayer.PlayerId;       // Can just use the same ID
                //networkEntityComp.OwnerType = NetworkOwnerType.Player;
                networkEntityComp.OwnerClientId = pendingPlayer.PlayerId;
                networkEntityComp.AssetId = assetDatabase.GetAssetIdFromUrlReference(prefabUrl);
                //networkEntityComp.IsLocalEntity = false;

                var networkPlayerComp = playerEntity.Get<NetworkPlayerComponent>();
                networkPlayerComp.PlayerName = pendingPlayer.PlayerName;

                var spawnPointEntity = gameplayScene.Entities.First(x => x.Name == "SpawnPoint");       // TODO: probably shouldn't hardcode it like this...
                playerEntity.Transform.Position = spawnPointEntity.Transform.Position;
                _networkEntityProcessor.AddAndRegisterEntity(playerEntity, gameplayScene, simTickNumber);
                // Note this must be set AFTER being added to the scene due to snapshot buffer needing to be instantiated by the processor
                var movementSnapshotComp = playerEntity.Get<MovementSnapshotsComponent>();
                MovementSnapshotsProcessor.CreateNewSnapshotData(simTickNumber, movementSnapshotComp, playerEntity.Transform);

                var newPlayer = new ServerActivePlayer(pendingPlayer.PlayerId, pendingPlayer.PlayerName, playerEntity, pendingPlayer.Connection);
                ActivePlayers.Add(newPlayer);
                // Notify the new player of all existing players
                for (int i = 0; i < ActivePlayers.Count - 1; i++)       // Exclude the last because that's the new player
                {
                    ref var existingPlayer = ref ActivePlayers.Items[i];
                    Debug.Assert(existingPlayer.PlayerId != newPlayer.PlayerId);

                    var existingPlayerDetails = entityExistenceStates[existingPlayer.PlayerEntity];
                    var spawnPlayer = new SpawnRemotePlayerMessage
                    {
                        PlayerId = existingPlayer.PlayerId,
                        SimulationTickNumber = existingPlayerDetails.SimulationTickNumberCreated,
                        PlayerName = existingPlayer.PlayerName,
                        Position = existingPlayer.PlayerEntity.Transform.Position,
                        Rotation = existingPlayer.PlayerEntity.Transform.Rotation
                    };
                    networkMessageWriter.Reset();
                    spawnPlayer.WriteTo(networkMessageWriter);
                    var conn = newPlayer.Connection;
                    conn.Send(networkMessageWriter, SendNetworkMessageType.ReliableOrdered);   // Use Ordered to ensure a player's joined & dropped events are in sequence
                }
                // Notify the new player of itself
                {
                    var spawnPlayer = new SpawnLocalPlayerMessage
                    {
                        PlayerId = newPlayer.PlayerId,
                        SimulationTickNumber = simTickNumber,
                        PlayerName = newPlayer.PlayerName,
                        Position = newPlayer.PlayerEntity.Transform.Position,
                        Rotation = newPlayer.PlayerEntity.Transform.Rotation
                    };
                    networkMessageWriter.Reset();
                    spawnPlayer.WriteTo(networkMessageWriter);
                    var conn = newPlayer.Connection;
                    conn.Send(networkMessageWriter, SendNetworkMessageType.ReliableOrdered);   // Use Ordered to ensure a player's joined & dropped events are in sequence
                }

                {   // Notify all existing players of this new player
                    var spawnPlayer = new SpawnRemotePlayerMessage
                    {
                        PlayerId = newPlayer.PlayerId,
                        SimulationTickNumber = simTickNumber,
                        PlayerName = newPlayer.PlayerName,
                        Position = newPlayer.PlayerEntity.Transform.Position,
                        Rotation = newPlayer.PlayerEntity.Transform.Rotation
                    };
                    networkMessageWriter.Reset();
                    spawnPlayer.WriteTo(networkMessageWriter);
                    for (int i = 0; i < ActivePlayers.Count - 1; i++)       // Exclude the last because that's the new player
                    {
                        var conn = ActivePlayers.Items[i].Connection;
                        conn.Send(networkMessageWriter, SendNetworkMessageType.ReliableOrdered);   // Use Ordered to ensure a player's joined & dropped events are in sequence
                    }
                }
            }

            internal void UpdatePlayerInputs(SimulationTickNumber inputSimTickNumber)
            {
                for (int i = 0; i < ActivePlayers.Count; i++)
                {
                    ref var player = ref ActivePlayers.Items[i];
                    bool wasInputApplied = false;
                    const int MaxPendingInputs = 5;
                    while (player.PendingInputs.Count > MaxPendingInputs)
                    {
                        // Discard inputs... TODO should probably tell the player input was rejected
                        player.PendingInputs.RemoveAt(0);
                    }
                    foreach (var kv in player.PendingInputs)
                    {
                        var inputSnapshotsComponent = player.InputSnapshotsComponent;
                        var inputFindResult = inputSnapshotsComponent.SnapshotStore.TryFindSnapshot(inputSimTickNumber);
                        Debug.Assert(inputFindResult.IsFound);
                        inputFindResult.Result = kv.Value;
                        inputFindResult.Result.SimulationTickNumber = inputSimTickNumber;   // SimTickNo not set on pending inputs, so need to copy it back over.
                        inputSnapshotsComponent.ServerLastAppliedPlayerInputSequenceNumber = inputFindResult.Result.PlayerInputSequenceNumber;
                        wasInputApplied = true;
                        break;  // Only want the first item
                    }
                    if (wasInputApplied)
                    {
                        player.PendingInputs.RemoveAt(0);
                    }
                    else
                    {
                        // TODO: Tell player input is missing
                    }
#if DEBUG
                    //if (i == 0)
                    //{
                    //    Debug.WriteLine($"InpProc: PendingInputsCount: {player.PendingInputs.Count}");
                    //}
#endif
                }
            }

            internal void SendEntityChangesToClients(SimulationTickNumber simTickNumber, NetworkMessageWriter networkMessageWriter)
            {
                for (int playerIdx = 0; playerIdx < ActivePlayers.Count; playerIdx++)
                {
                    ref var player = ref ActivePlayers.Items[playerIdx];
                    var conn = player.Connection;

                    networkMessageWriter.Reset();

                    var snaphotUpdatesMsg = new EntitySnaphotUpdatesMessage
                    {
                        AcknowledgedLastReceivedPlayerInputSequenceNumber = player.InputSnapshotsComponent.ServerLastAcknowledgedPlayerInputSequenceNumber,
                        LastAppliedServerPlayerInputSequenceNumber = player.InputSnapshotsComponent.ServerLastAppliedPlayerInputSequenceNumber,
                        ServerSimulationTickNumber = simTickNumber
                    };
                    snaphotUpdatesMsg.WriteTo(networkMessageWriter);

                    _workingUpdateEntities.Clear();
                    _workingUpdateEntityInputs.Clear();

                    var entityExistenceStates = _networkEntityProcessor._entityExistenceStates;
                    foreach (var entityExistence in entityExistenceStates.Values)
                    {
                        _workingUpdateEntities.Add(entityExistence);
                        if (entityExistence.InputSnapshotsComponent != null)
                        {
                            var inputSnapshotStore = entityExistence.InputSnapshotsComponent.SnapshotStore.GetOrCreate(simTickNumber);
                            if (inputSnapshotStore.IsJumpButtonDown)
                            {
                                var entInput = new EntityUpdateInputAction
                                {
                                    SimulationTickNumber = simTickNumber,
                                    NetworkEntityId = entityExistence.NetworkEntityComponent.NetworkEntityId,
                                    InputActionType = InputActionType.Jump
                                };
                                _workingUpdateEntityInputs.Add(entInput);
                            }
                        }
                    }

                    {   // Update entity transforms
                        var updateTransformMsg = new EntityUpdateTransform
                        {
                            SimulationTickNumber = simTickNumber
                        };
                        updateTransformMsg.WriteHeader(networkMessageWriter, (ushort)_workingUpdateEntities.Count);
                        for (int i = 0; i < _workingUpdateEntities.Count; i++)
                        {
                            var ent = _workingUpdateEntities[i];
                            updateTransformMsg.NetworkEntityId = ent.NetworkEntityComponent.NetworkEntityId;

                            var movementSnapshotStore = ent.MovementSnapshotsComponent.SnapshotStore.GetOrCreate(simTickNumber);
                            updateTransformMsg.Position = movementSnapshotStore.LocalPosition;
                            updateTransformMsg.YawOrientation = movementSnapshotStore.YawOrientation;
                            updateTransformMsg.MoveSpeedDecimalPercentage = movementSnapshotStore.MoveSpeedDecimalPercentage;
                            updateTransformMsg.CurrentMoveInputVelocity = movementSnapshotStore.CurrentMoveInputVelocity;
                            updateTransformMsg.IsGrounded = movementSnapshotStore.IsGrounded;
                            updateTransformMsg.PhysicsEngineLinearVelocity = movementSnapshotStore.PhysicsEngineLinearVelocity;

                            updateTransformMsg.WriteNextArrayItem(networkMessageWriter);
                        }
                    }
                    {   // Update entity inputs/actions
                        var updateInputMsg = new EntityUpdateInputAction();
                        updateInputMsg.WriteHeader(networkMessageWriter, (ushort)_workingUpdateEntityInputs.Count);
                        for (int i = 0; i < _workingUpdateEntityInputs.Count; i++)
                        {
                            var ent = _workingUpdateEntityInputs[i];
                            updateInputMsg.SimulationTickNumber = ent.SimulationTickNumber;
                            updateInputMsg.NetworkEntityId = ent.NetworkEntityId;
                            updateInputMsg.InputActionType = ent.InputActionType;

                            updateInputMsg.WriteNextArrayItem(networkMessageWriter);
                        }
                    }

                    conn.Send(networkMessageWriter, SendNetworkMessageType.Unreliable);
                }
            }

            internal void AddPendingPlayer(ServerPendingPlayer player)
            {
                //lock (_serverPlayerManager.PendingPlayers)
                PendingPlayers.Add(player);
            }

            internal void RemovePlayer(SerializableGuid playerId, NetworkMessageWriter networkMessageWriter)
            {
                int clientIndex = ActivePlayers.FindIndex(x => x.PlayerId == playerId);
                if (clientIndex < 0)
                {
                    // Player wasn't fully added yet, can be removed without notifying any other players
                    clientIndex = PendingPlayers.FindIndex(x => x.PlayerId == playerId);
                    Debug.Assert(clientIndex >= 0);
                    PendingPlayers.RemoveAt(clientIndex);
                    return;
                }
                Debug.Assert(clientIndex >= 0);
                var player = ActivePlayers[clientIndex];
                ActivePlayers.RemoveAt(clientIndex);

                var gameplayScene = _networkEntityProcessor._lazyLoadedScene.GetGameplayScene();
                _networkEntityProcessor.RemoveAndUnregisterEntity(playerId, player.PlayerEntity, gameplayScene);

                // Notify all players of this player's removal
                var entityExistenceStates = _networkEntityProcessor._entityExistenceStates;
                var gameClockManager = _networkEntityProcessor._gameClockManager;

                var despawnPlayer = new DespawnRemotePlayerMessage
                {
                    PlayerId = playerId,
                    SimulationTickNumber = gameClockManager.SimulationClock.SimulationTickNumber
                };
                networkMessageWriter.Reset();
                despawnPlayer.WriteTo(networkMessageWriter);
                for (int i = 0; i < ActivePlayers.Count; i++)
                {
                    ref var existingPlayer = ref ActivePlayers.Items[i];
                    var existingPlayerDetails = entityExistenceStates[existingPlayer.PlayerEntity];
                    var conn = ActivePlayers.Items[i].Connection;
                    conn.Send(networkMessageWriter, SendNetworkMessageType.ReliableOrdered);   // Use Ordered to ensure a player's joined & dropped events are in sequence
                }
            }

            internal void CollectPendingInputs(
                SerializableGuid playerId,
                PlayerUpdateMessage pendingPlayerUpdateMessage,
                FastList<PlayerUpdateInputMessage> pendingPlayerUpdateInputsMessages)
            {
                int clientIndex = ActivePlayers.FindIndex(x => x.PlayerId == playerId);
                Debug.Assert(clientIndex >= 0);
                ref var player = ref ActivePlayers.Items[clientIndex];
                var playerNetworkEntityComp = player.NetworkEntityComponent;
                if (playerNetworkEntityComp.LastAcknowledgedServerSimulationTickNumber < pendingPlayerUpdateMessage.AcknowledgedServerSimulationTickNumber)
                {
                    playerNetworkEntityComp.LastAcknowledgedServerSimulationTickNumber = pendingPlayerUpdateMessage.AcknowledgedServerSimulationTickNumber;
                }

                var inputSnapshotsComp = player.InputSnapshotsComponent;
                int inputCount = pendingPlayerUpdateInputsMessages.Count;
                var inputArray = pendingPlayerUpdateInputsMessages.Items;
                for (int i = 0; i < inputCount; i++)
                {
                    ref var playerUpdateMsg = ref inputArray[i];
                    var playerInputSeqNumber = playerUpdateMsg.PlayerInputSequenceNumber;
                    //if (_gameClockManager.SimulationTickNumber <= simTickNumber)
                    //{
                    //    // TODO: if this is a new message but is too far behind, tell player
                    //    // we rejected the player's input
                    //    continue;
                    //}
                    if (inputSnapshotsComp.ServerLastAcknowledgedPlayerInputSequenceNumber < playerInputSeqNumber
                        && !player.PendingInputs.ContainsKey(playerInputSeqNumber))
                    {
                        var inputCmdSet = new InputSnapshotsComponent.InputCommandSet
                        {
                            //SimulationTickNumber not determined yet
                            PlayerInputSequenceNumber = playerUpdateMsg.PlayerInputSequenceNumber,
                            MoveInput = playerUpdateMsg.MoveInput,
                            IsJumpButtonDown = playerUpdateMsg.JumpRequestedInput,
                        };
                        player.PendingInputs.Add(playerUpdateMsg.PlayerInputSequenceNumber, inputCmdSet);
                    }
                }

                var lastReceivedPlayerInputSeqNo = inputCount > 0 ? pendingPlayerUpdateInputsMessages[inputCount - 1].PlayerInputSequenceNumber : default;
                if (inputSnapshotsComp.ServerLastAcknowledgedPlayerInputSequenceNumber < lastReceivedPlayerInputSeqNo)
                {
                    inputSnapshotsComp.ServerLastAcknowledgedPlayerInputSequenceNumber = lastReceivedPlayerInputSeqNo;
                }
            }

            internal void SetPlayerReady(SerializableGuid playerId)
            {
                //lock (_serverPlayerManager.PendingPlayers)
                int clientIndex = PendingPlayers.FindIndex(x => x.PlayerId == playerId);
                Debug.Assert(clientIndex >= 0);
                ref var player = ref PendingPlayers.Items[clientIndex];
                player.PendingPlayerState = PendingPlayerState.Ready;
            }
        }

        private struct ServerPendingPlayer
        {
            public readonly SerializableGuid PlayerId;
            public readonly string PlayerName;
            public readonly NetworkConnection Connection;
            public PendingPlayerState PendingPlayerState;

            public ServerPendingPlayer(SerializableGuid playerId, string playerName, NetworkConnection connection)
            {
                PlayerId = playerId;
                PlayerName = playerName;
                Connection = connection;
                PendingPlayerState = PendingPlayerState.JustConnected;
            }
        }

        private readonly struct ServerActivePlayer
        {
            /// <summary>
            /// This is the same as the NetworkEntityId.
            /// </summary>
            public readonly SerializableGuid PlayerId;
            public readonly string PlayerName;
            public readonly Entity PlayerEntity;
            public readonly NetworkEntityComponent NetworkEntityComponent;
            public readonly InputSnapshotsComponent InputSnapshotsComponent;
            public readonly MovementSnapshotsComponent MovementSnapshotsComponent;
            public readonly Stride.Core.Collections.SortedList<PlayerInputSequenceNumber, InputSnapshotsComponent.InputCommandSet> PendingInputs;
            public readonly NetworkConnection Connection;

            public ServerActivePlayer(SerializableGuid playerId, string playerName, Entity playerEntity, NetworkConnection connection)
            {
                PlayerId = playerId;
                PlayerName = playerName;
                PlayerEntity = playerEntity;
                NetworkEntityComponent = playerEntity.Get<NetworkEntityComponent>();
                InputSnapshotsComponent = playerEntity.Get<InputSnapshotsComponent>();
                Debug.Assert(InputSnapshotsComponent != null, $"{InputSnapshotsComponent} must exist.");
                MovementSnapshotsComponent = playerEntity.Get<MovementSnapshotsComponent>();
                PendingInputs = new Stride.Core.Collections.SortedList<PlayerInputSequenceNumber, InputSnapshotsComponent.InputCommandSet>();
                Connection = connection;
            }
        }
    }
}
