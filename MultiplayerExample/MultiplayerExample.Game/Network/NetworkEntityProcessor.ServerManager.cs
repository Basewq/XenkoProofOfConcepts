using MultiplayerExample.Core;
using MultiplayerExample.GameServices;
using MultiplayerExample.Network.EntityMessages;
using MultiplayerExample.Network.NetworkMessages;
using MultiplayerExample.Network.NetworkMessages.Client;
using MultiplayerExample.Network.NetworkMessages.Server;
using MultiplayerExample.Network.SnapshotStores;
using MultiplayerExample.Utilities;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MultiplayerExample.Network
{
    partial class NetworkEntityProcessor
    {
        /// <summary>
        /// Manager for all players when this game is being hosted or is a dedicated server.
        /// This is just a lightweight container to contain all server related functions.
        /// </summary>
        internal class ServerPlayerManager
        {
            private readonly NetworkEntityProcessor _networkEntityProcessor;

            private readonly List<EntityExistenceDetails> _workingUpdateEntities;
            private readonly List<EntityUpdateInputAction> _workingUpdateEntityInputs;

            public readonly FastList<ServerPendingRemotePlayer> PendingRemotePlayers;
            public readonly FastList<ServerActiveRemotePlayer> ActiveRemotePlayers;
            public readonly FastList<ServerActiveLocalPlayer> ActiveLocalPlayers;

            public ServerPlayerManager(int initialCapacity, NetworkEntityProcessor networkEntityProcessor)
            {
                PendingRemotePlayers = new FastList<ServerPendingRemotePlayer>(initialCapacity);
                ActiveRemotePlayers = new FastList<ServerActiveRemotePlayer>(initialCapacity);
                ActiveLocalPlayers = new FastList<ServerActiveLocalPlayer>(initialCapacity);

                _networkEntityProcessor = networkEntityProcessor;

                _workingUpdateEntities = new List<EntityExistenceDetails>();
                _workingUpdateEntityInputs = new List<EntityUpdateInputAction>();
            }

            internal void UpdatePendingPlayerStates(NetworkMessageWriter networkMessageWriter)
            {
                var networkAssetDatabase = _networkEntityProcessor._networkAssetDatabase;
                var assetDefinitions = GetNetworkAssetDefinitions();

                //lock (PendingPlayers)
                for (int i = 0; i < PendingRemotePlayers.Count; i++)
                {
                    ref var pendingPlayer = ref PendingRemotePlayers.Items[i];
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
                            OnNewRemotePlayerReady(pendingPlayer.PlayerId, pendingPlayer.PlayerName, pendingPlayer.Connection, networkMessageWriter);
                            break;
                        default:
                            break;
                    }
                }
                PendingRemotePlayers.RemoveAll(x => x.PendingPlayerState == PendingPlayerState.Ready);
            }

            private void OnNewRemotePlayerReady(
                SerializableGuid playerId, string playerName, NetworkConnection playerConnection,
                NetworkMessageWriter networkMessageWriter)
            {
                var assetDefinitions = GetNetworkAssetDefinitions();
                var assetDatabase = _networkEntityProcessor._networkAssetDatabase;
                var networkService = _networkEntityProcessor._networkService;
                var content = _networkEntityProcessor._content;
                var gameClockManager = _networkEntityProcessor._gameClockManager;

                var entityExistenceStates = _networkEntityProcessor._entityExistenceStates;

                var gameplayScene = GetGameplayScene();

                var simTickNumber = gameClockManager.SimulationClock.SimulationTickNumber;
                // Can add to the scene now
                Entity playerEntity;
                if (networkService.NetworkGameMode == NetworkGameMode.DedicatedServer)
                {
                    var playerPrefabUrl = assetDefinitions.PlayerAssets.ServerRemotePlayer;
                    var playerPrefab = content.Load(playerPrefabUrl);
                    playerEntity = playerPrefab.InstantiateSingle();

                    var networkEntityComp = playerEntity.Get<NetworkEntityComponent>();
                    networkEntityComp.NetworkEntityId = playerId;       // Can just use the same ID
                    networkEntityComp.OwnerClientId = playerId;
                    networkEntityComp.AssetId = assetDatabase.GetAssetIdFromUrlReference(playerPrefabUrl);

                    var networkPlayerComp = playerEntity.Get<NetworkPlayerComponent>();
                    networkPlayerComp.PlayerName = playerName;

                    GetPlayerSpawnLocation(gameplayScene, out var spawnPosition, out var spawnRotation);
                    playerEntity.Transform.Position = spawnPosition;
                    playerEntity.Transform.Rotation = spawnRotation;
                    _networkEntityProcessor.AddAndRegisterEntity(playerEntity, gameplayScene, simTickNumber);
                }
                else
                {
                    // Client game, so has the prefab also player view entity
                    GetPlayerSpawnLocation(gameplayScene, out var spawnPosition, out var spawnRotation);
                    playerEntity = _networkEntityProcessor.CreateAndAddClientPlayerEntity(simTickNumber, playerId, playerName, ref spawnPosition, ref spawnRotation, isLocalEntity: false);
                }

                // Note this must be set AFTER being added to the scene due to snapshot buffer needing to be instantiated by the processor
                var movementSnapshotComp = playerEntity.Get<MovementSnapshotsComponent>();
                MovementSnapshotsProcessor.CreateNewSnapshotData(simTickNumber, movementSnapshotComp, playerEntity.Transform);

                var newPlayer = new ServerActiveRemotePlayer(playerId, playerName, playerEntity, playerConnection);
                ActiveRemotePlayers.Add(newPlayer);

                var gameManager = GetGameManager();
                gameManager.RaisePlayerAddedEvent(playerEntity);

                // Notify the new player of all existing players
                for (int i = 0; i < ActiveRemotePlayers.Count - 1; i++)       // Exclude the last because that's the new player
                {
                    ref var existingPlayer = ref ActiveRemotePlayers.Items[i];
                    Debug.Assert(existingPlayer.PlayerId != playerId);

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
                    playerConnection.Send(networkMessageWriter, SendNetworkMessageType.ReliableOrdered);   // Use Ordered to ensure a player's joined & dropped events are in sequence
                }
                for (int i = 0; i < ActiveLocalPlayers.Count; i++)
                {
                    ref var existingPlayer = ref ActiveLocalPlayers.Items[i];
                    Debug.Assert(existingPlayer.PlayerId != playerId);

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
                    playerConnection.Send(networkMessageWriter, SendNetworkMessageType.ReliableOrdered);   // Use Ordered to ensure a player's joined & dropped events are in sequence
                }
                // Notify the new player of itself
                {
                    var spawnPlayer = new SpawnLocalPlayerMessage
                    {
                        PlayerId = playerId,
                        SimulationTickNumber = simTickNumber,
                        PlayerName = playerName,
                        Position = playerEntity.Transform.Position,
                        Rotation = playerEntity.Transform.Rotation
                    };
                    networkMessageWriter.Reset();
                    spawnPlayer.WriteTo(networkMessageWriter);
                    playerConnection.Send(networkMessageWriter, SendNetworkMessageType.ReliableOrdered);   // Use Ordered to ensure a player's joined & dropped events are in sequence
                }

                SendSpawnNewPlayerToRemotePlayers(playerId, playerName, playerEntity, simTickNumber, networkMessageWriter);
            }

            internal void UpdatePlayerInputs(SimulationTickNumber inputSimTickNumber)
            {
                for (int i = 0; i < ActiveRemotePlayers.Count; i++)
                {
                    ref var player = ref ActiveRemotePlayers.Items[i];
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
                for (int playerIdx = 0; playerIdx < ActiveRemotePlayers.Count; playerIdx++)
                {
                    ref var player = ref ActiveRemotePlayers.Items[playerIdx];
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

            internal void AddPendingRemotePlayer(SerializableGuid playerId, string playerName, NetworkConnection connection)
            {
                var player = new ServerPendingRemotePlayer(playerId, playerName, connection);
                //lock (_serverPlayerManager.PendingPlayers)
                PendingRemotePlayers.Add(player);
            }

            /// <returns>True if the removed player was an active player (ie. was in-game)</returns>
            internal bool RemoveRemotePlayer(SerializableGuid playerId)
            {
                int clientIndex = ActiveRemotePlayers.FindIndex(x => x.PlayerId == playerId);
                if (clientIndex < 0)
                {
                    // Player wasn't fully added yet, can be removed without notifying any other players
                    clientIndex = PendingRemotePlayers.FindIndex(x => x.PlayerId == playerId);
                    Debug.Assert(clientIndex >= 0);
                    PendingRemotePlayers.RemoveAt(clientIndex);
                    return false;
                }
                Debug.Assert(clientIndex >= 0);
                var playerEntity = ActiveRemotePlayers.Items[clientIndex].PlayerEntity;
                ActiveRemotePlayers.RemoveAt(clientIndex);

                var gameplayScene = GetGameplayScene();
                _networkEntityProcessor.RemoveAndUnregisterEntity(playerId, playerEntity, gameplayScene);
                var gameManager = GetGameManager();
                gameManager.RaisePlayerRemovedEntity(playerEntity);
                return true;
            }

            internal void AddLocalPlayer(SerializableGuid playerId, string playerName, NetworkMessageWriter networkMessageWriter)
            {
                Debug.Assert(_networkEntityProcessor._networkService.NetworkGameMode == NetworkGameMode.Local || _networkEntityProcessor._networkService.NetworkGameMode == NetworkGameMode.ListenServer, "Can only add local player when running a local game or hosting a game.");
                var gameplayScene = GetGameplayScene();
                var gameClockManager = _networkEntityProcessor._gameClockManager;
                var simTickNumber = gameClockManager.SimulationClock.SimulationTickNumber;

                GetPlayerSpawnLocation(gameplayScene, out var spawnPosition, out var spawnRotation);
                var playerEntity = _networkEntityProcessor.CreateAndAddClientPlayerEntity(simTickNumber, playerId, playerName, ref spawnPosition, ref spawnRotation, isLocalEntity: true);

                var newPlayer = new ServerActiveLocalPlayer(playerId, playerName, playerEntity);
                ActiveLocalPlayers.Add(newPlayer);

                var gameManager = GetGameManager();
                gameManager.RaisePlayerAddedEvent(playerEntity);

                SendSpawnNewPlayerToRemotePlayers(playerId, playerName, playerEntity, simTickNumber, networkMessageWriter);
            }

            private void SendSpawnNewPlayerToRemotePlayers(
                SerializableGuid playerId, string playerName, Entity playerEntity, SimulationTickNumber simTickNumber,
                NetworkMessageWriter networkMessageWriter)
            {
                if (ActiveRemotePlayers.Count > 0)
                {
                    // Notify all existing remote players of this new player
                    var spawnPlayer = new SpawnRemotePlayerMessage
                    {
                        PlayerId = playerId,
                        SimulationTickNumber = simTickNumber,
                        PlayerName = playerName,
                        Position = playerEntity.Transform.Position,
                        Rotation = playerEntity.Transform.Rotation
                    };
                    networkMessageWriter.Reset();
                    spawnPlayer.WriteTo(networkMessageWriter);
                    for (int i = 0; i < ActiveRemotePlayers.Count - 1; i++)       // Exclude the last because that's the new player
                    {
                        var conn = ActiveRemotePlayers.Items[i].Connection;
                        conn.Send(networkMessageWriter, SendNetworkMessageType.ReliableOrdered);   // Use Ordered to ensure a player's joined & dropped events are in sequence
                    }
                }
            }

            internal void RemoveLocalPlayer(SerializableGuid playerId)
            {
                int clientIndex = ActiveLocalPlayers.FindIndex(x => x.PlayerId == playerId);
                Debug.Assert(clientIndex >= 0);
                var playerEntity = ActiveLocalPlayers.Items[clientIndex].PlayerEntity;
                ActiveLocalPlayers.RemoveAt(clientIndex);

                var gameplayScene = GetGameplayScene();
                _networkEntityProcessor.RemoveAndUnregisterEntity(playerId, playerEntity, gameplayScene);
                var gameManager = GetGameManager();
                gameManager.RaisePlayerRemovedEntity(playerEntity);
            }

            internal void CollectPendingInputs(
                SerializableGuid playerId,
                PlayerUpdateMessage pendingPlayerUpdateMessage,
                FastList<PlayerUpdateInputMessage> pendingPlayerUpdateInputsMessages)
            {
                int clientIndex = ActiveRemotePlayers.FindIndex(x => x.PlayerId == playerId);
                Debug.Assert(clientIndex >= 0);
                ref var player = ref ActiveRemotePlayers.Items[clientIndex];
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
                int clientIndex = PendingRemotePlayers.FindIndex(x => x.PlayerId == playerId);
                Debug.Assert(clientIndex >= 0);
                ref var player = ref PendingRemotePlayers.Items[clientIndex];
                player.PendingPlayerState = PendingPlayerState.Ready;
            }

            private void GetPlayerSpawnLocation(Scene gameplayScene, out Vector3 position, out Quaternion rotation)
            {
                var spawnPointEntity = gameplayScene.Entities.First(x => x.Name == "SpawnPoint");       // TODO: probably shouldn't hardcode it like this...
                position = spawnPointEntity.Transform.Position;
                rotation = spawnPointEntity.Transform.Rotation;
            }

            private GameManager GetGameManager() => _networkEntityProcessor._lazyLoadedScene.GetGameManager();
            private Scene GetGameplayScene() => _networkEntityProcessor._lazyLoadedScene.GetGameplayScene();
            private NetworkAssetDefinitions GetNetworkAssetDefinitions() => _networkEntityProcessor._lazyLoadedScene.GetNetworkAssetDefinitions();
        }

        internal struct ServerPendingRemotePlayer
        {
            public readonly SerializableGuid PlayerId;
            public readonly string PlayerName;
            public readonly NetworkConnection Connection;
            public PendingPlayerState PendingPlayerState;

            public ServerPendingRemotePlayer(SerializableGuid playerId, string playerName, NetworkConnection connection)
            {
                PlayerId = playerId;
                PlayerName = playerName;
                Connection = connection;
                PendingPlayerState = PendingPlayerState.JustConnected;
            }
        }

        internal enum PendingPlayerState
        {
            JustConnected,
            LoadingScene,
            Ready
        }

        internal readonly struct ServerActiveRemotePlayer
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

            public ServerActiveRemotePlayer(SerializableGuid playerId, string playerName, Entity playerEntity, NetworkConnection connection)
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

        internal readonly struct ServerActiveLocalPlayer
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

            public ServerActiveLocalPlayer(SerializableGuid playerId, string playerName, Entity playerEntity)
            {
                PlayerId = playerId;
                PlayerName = playerName;
                PlayerEntity = playerEntity;
                NetworkEntityComponent = playerEntity.Get<NetworkEntityComponent>();
                InputSnapshotsComponent = playerEntity.Get<InputSnapshotsComponent>();
                Debug.Assert(InputSnapshotsComponent != null, $"{InputSnapshotsComponent} must exist.");
                MovementSnapshotsComponent = playerEntity.Get<MovementSnapshotsComponent>();
            }
        }
    }
}
