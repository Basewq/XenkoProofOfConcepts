using MultiplayerExample.Core;
using MultiplayerExample.Engine;
using MultiplayerExample.GameServices;
using MultiplayerExample.GameServices.SceneHandlers;
using MultiplayerExample.Network.EntityMessages;
using MultiplayerExample.Network.NetworkMessages;
using MultiplayerExample.Network.NetworkMessages.Client;
using MultiplayerExample.Network.NetworkMessages.Server;
using MultiplayerExample.Network.SnapshotStores;
using MultiplayerExample.Utilities;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Games;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MultiplayerExample.Network
{
    class NetworkEntityProcessor : EntityProcessor<NetworkEntityComponent, NetworkEntityProcessor.AssociatedData>,
        INetworkPreUpdateProcessor, INetworkPostUpdateProcessor
    {
        private readonly FastList<EntityUpdateTransform> _clientUpdateEntityTransforms = new FastList<EntityUpdateTransform>();
        private readonly FastList<EntityUpdateInputAction> _clientUpdateEntityInputs = new FastList<EntityUpdateInputAction>();

        private readonly ServerPlayerManager _serverPlayerManager;
        private readonly ClientPlayerManager _clientPlayerManager;

        private readonly Dictionary<SerializableGuid, AssociatedData> _networkEntityIdToEntityDataMap = new Dictionary<SerializableGuid, AssociatedData>();
        private readonly Dictionary<Entity, EntityExistenceDetails> _entityExistenceStates = new Dictionary<Entity, EntityExistenceDetails>();

        private GameClockManager _gameClockManager;
        private GameEngineContext _gameEngineContext;

        private IGameNetworkService _networkService;
        private NetworkMessageWriter _networkMessageWriter;
        private SceneSystem _sceneSystem;

        private Entity _gameManagerEntity;
        private GameManager _gameManager;
        private LazyLoadedSceneData _lazyLoadedScene;

        private NetworkAssetDefinitions _assetDefinitions;
        private NetworkAssetDatabase _networkAssetDatabase;

        private ContentManager _content;

        public bool IsEnabled { get; set; }

        public event Action<Entity> PlayerAdded;
        public event Action<Entity> PlayerRemoved;

        public NetworkEntityProcessor()
        {
            _networkMessageWriter = new NetworkMessageWriter(new LiteNetLib.Utils.NetDataWriter());
            _serverPlayerManager = new ServerPlayerManager(4, this);
            _clientPlayerManager = new ClientPlayerManager(this);

            _lazyLoadedScene = new LazyLoadedSceneData(this);

            Order = -20000;         // Ensure this occurs before other processors
            // Not using Enabled property, because that completely disables the processor, where it doesn't even pick up newly added entities
            IsEnabled = true;
        }

        protected override void OnSystemAdd()
        {
            _gameClockManager = Services.GetService<GameClockManager>();
            _gameEngineContext = Services.GetService<GameEngineContext>();
            _networkService = Services.GetSafeServiceAs<IGameNetworkService>();
            _sceneSystem = Services.GetSafeServiceAs<SceneSystem>();
            _content = Services.GetSafeServiceAs<ContentManager>();
            _networkAssetDatabase = Services.GetSafeServiceAs<NetworkAssetDatabase>();
        }

        protected override void OnSystemRemove()
        {
        }

        //protected override void OnEntityComponentAdding(Entity entity, [NotNull] NetworkEntityComponent component, [NotNull] AssociatedData data)
        //{
        //}

        protected override AssociatedData GenerateComponentData([NotNull] Entity entity, [NotNull] NetworkEntityComponent component)
        {
            return new AssociatedData
            {
                // Can also add other info/components here
                Entity = entity,
                TransformComponent = entity.Transform,
                NetworkEntityComponent = entity.Get<NetworkEntityComponent>(),
                MovementSnapshotsComponent = entity.Get<MovementSnapshotsComponent>(),
                ClientPredictionSnapshotsComponent = entity.Get<ClientPredictionSnapshotsComponent>()
            };
        }

        protected override bool IsAssociatedDataValid([NotNull] Entity entity, [NotNull] NetworkEntityComponent component, [NotNull] AssociatedData associatedData)
        {
            return associatedData.TransformComponent == entity.Transform
                && associatedData.MovementSnapshotsComponent == entity.Get<MovementSnapshotsComponent>()
                && associatedData.NetworkEntityComponent == entity.Get<NetworkEntityComponent>()
                && associatedData.ClientPredictionSnapshotsComponent == entity.Get<ClientPredictionSnapshotsComponent>();
        }

        public void PreUpdate(GameTime gameTime)
        {
            // Lazy load gameplay related systems
            var rootScene = _sceneSystem.SceneInstance.RootScene;
            if (rootScene == null)
            {
                return;     // Not set up yet
            }
            if (_gameManagerEntity == null)
            {
                var entityManager = rootScene.Entities;
                _gameManagerEntity = entityManager.First(x => x.Name == GameManager.EntityName);      // This entity must exist in the root scene!
                Debug.Assert(_gameManagerEntity != null, $"Root scene must contain GameManager entity.");
                _gameManager = _gameManagerEntity.Get<GameManager>();
                Debug.Assert(_gameManager != null, $"GameManager entity must contain {nameof(GameManager)} component.");
                _assetDefinitions = _gameManagerEntity.Get<NetworkAssetDefinitions>();
                Debug.Assert(_gameManagerEntity != null, $"GameManager entity must contain {nameof(NetworkAssetDefinitions)} component.");
                _assetDefinitions.LoadAssetIds(_networkAssetDatabase);
            }

            if (_networkService.NetworkGameMode == NetworkGameMode.ListenServer
                || _networkService.NetworkGameMode == NetworkGameMode.DedicatedServer)
            {
                _serverPlayerManager.UpdatePendingPlayerStates(_networkMessageWriter);

                if (_gameClockManager.SimulationClock.IsNextSimulation)
                {
                    var inputSimTickNumber = _gameClockManager.SimulationClock.SimulationTickNumber;
                    _serverPlayerManager.UpdatePlayerInputs(inputSimTickNumber);
                }
            }
        }

        public void PostUpdate(GameTime gameTime)
        {
            if (!_gameClockManager.SimulationClock.IsNextSimulation)
            {
                // Only update during a simulation update since input is only processed at this time
                return;
            }

            var simTickNumber = _gameClockManager.SimulationClock.SimulationTickNumber;

            switch (_networkService.NetworkGameMode)
            {
                case NetworkGameMode.NotSet:
                    break;
                case NetworkGameMode.Local:
                    break;
                case NetworkGameMode.ListenServer:
                    _serverPlayerManager.SendEntityChangesToClients(simTickNumber, _networkMessageWriter);
                    break;
                case NetworkGameMode.DedicatedServer:
                    _serverPlayerManager.SendEntityChangesToClients(simTickNumber, _networkMessageWriter);
                    break;
                case NetworkGameMode.RemoteClient:
                    _clientPlayerManager.SendClientInputPredictionsToServer(simTickNumber, _networkMessageWriter);
                    break;
                default:
                    Debug.Fail($"Unknown game mode type: {_networkService.NetworkGameMode}");
                    break;
            }
        }

        private void AddAndRegisterEntity(Entity entity, Scene gameplayScene, SimulationTickNumber simulationTickNumberCreated)
        {
            gameplayScene.Entities.Add(entity);
            var existenceDetails = new EntityExistenceDetails
            {
                Entity = entity,
                NetworkEntityComponent = entity.Get<NetworkEntityComponent>(),
                //TransformComponent = entity.Transform,
                MovementSnapshotsComponent = entity.Get<MovementSnapshotsComponent>(),
                InputSnapshotsComponent = entity.Get<InputSnapshotsComponent>(),
                SimulationTickNumberCreated = simulationTickNumberCreated,
            };
            _entityExistenceStates[entity] = existenceDetails;

            var data = ComponentDatas[existenceDetails.NetworkEntityComponent];
            _networkEntityIdToEntityDataMap.Add(existenceDetails.NetworkEntityComponent.NetworkEntityId, data);
        }

        internal void Server_AddPlayer(SerializableGuid playerId, string playerName, NetworkConnection connection)
        {
            var player = new ServerPendingPlayer(playerId, playerName, connection);
            _serverPlayerManager.AddPendingPlayer(player);
        }

        internal void Server_RemovePlayer(SerializableGuid playerId)
        {
            _serverPlayerManager.RemovePlayer(playerId, _networkMessageWriter);
        }

        private void RemoveAndUnregisterEntity(SerializableGuid playerId, Entity entity, Scene gameplayScene)
        {
            bool isRemovedFromEntityIdToEntityDataMap = _networkEntityIdToEntityDataMap.Remove(playerId);
            bool isRemovedFromExistanceStates = _entityExistenceStates.Remove(entity);
            bool isRemovedFromScene = gameplayScene.Entities.Remove(entity);
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

        internal void Client_SpawnLocalPlayer(NetworkMessageReader message, NetworkConnection connectionToServer)
        {
            var gameplayScene = _lazyLoadedScene.GetGameplayScene();
            _clientPlayerManager.SpawnLocalPlayer(message, gameplayScene, connectionToServer);
        }

        internal void Client_SpawnRemotePlayer(NetworkMessageReader message)
        {
            var gameplayScene = _lazyLoadedScene.GetGameplayScene();
            _clientPlayerManager.SpawnRemotePlayer(message, gameplayScene);
        }

        internal void Client_DespawnRemotePlayer(NetworkMessageReader message)
        {
            var gameplayScene = _lazyLoadedScene.GetGameplayScene();
            _clientPlayerManager.DespawnRemotePlayer(message, gameplayScene);
        }

        internal void Client_UpdateStates(NetworkMessageReader message)
        {
            EntitySnaphotUpdatesMessage msg = default;
            if (msg.TryRead(message))
            {
                var hasServerAppliedNewPlayerInput = _clientPlayerManager.AcknowledgeReceivedPlayerInputs(msg.AcknowledgedLastReceivedPlayerInputSequenceNumber, msg.LastAppliedServerPlayerInputSequenceNumber);
                _gameClockManager.NetworkServerSimulationClock.SetClockFromTickNumber(msg.ServerSimulationTickNumber);

                _clientUpdateEntityTransforms.Clear();
                _clientUpdateEntityInputs.Clear();
                PopulateMessages(message, _clientUpdateEntityTransforms);
                PopulateMessages(message, _clientUpdateEntityInputs);

                if (_clientUpdateEntityTransforms.Count > 0 || _clientUpdateEntityInputs.Count > 0)
                {
                    //var simTickNumber = msg.ServerSimulationTickNumber;     // TODO: Should be gameclock sim tick???
                    var simTickNumber = _gameClockManager.SimulationClock.SimulationTickNumber;
                    _clientPlayerManager.UpdateEntityStates(
                        simTickNumber, hasServerAppliedNewPlayerInput, _clientUpdateEntityTransforms, _clientUpdateEntityInputs, msg.LastAppliedServerPlayerInputSequenceNumber);
                }

                //Debug.WriteLine($"Cln ProcessData.ProcessMessageSnapshotUpdates: {_workingUpdateEntityTransforms.Count}");
            }

            static bool PopulateMessages<TMsg>(NetworkMessageReader netMessage, FastList<TMsg> messageList)
                where TMsg : struct, INetworkMessageArray
            {
                messageList.Clear();
                TMsg msg = default;
                if (!msg.TryReadHeader(netMessage, out var msgCount))
                {
                    return false;
                }
                for (int i = 0; i < msgCount; i++)
                {
                    if (!msg.TryReadNextArrayItem(netMessage))
                    {
                        continue;
                    }
                    messageList.Add(msg);
                }
                return true;
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
            public Entity Entity;
            public TransformComponent TransformComponent;
            public NetworkEntityComponent NetworkEntityComponent;
            public MovementSnapshotsComponent MovementSnapshotsComponent;
            public ClientPredictionSnapshotsComponent ClientPredictionSnapshotsComponent;   // Optional component
        }

        private enum PendingPlayerState
        {
            JustConnected,
            LoadingScene,
            Ready
        }

        private class EntityExistenceDetails
        {
            public Entity Entity;
            public NetworkEntityComponent NetworkEntityComponent;
            //public TransformComponent TransformComponent;
            public MovementSnapshotsComponent MovementSnapshotsComponent;
            public InputSnapshotsComponent InputSnapshotsComponent;
            public SimulationTickNumber SimulationTickNumberCreated;
            //public SimulationTickNumber SimulationTickNumberRemoved;
        }

        /// <summary>
        /// Manager for all players when this game is being hosted or is a dedicated server.
        /// </summary>
        private class ServerPlayerManager
        {
            private readonly NetworkEntityProcessor _networkEntityProcessor;

            private readonly List<EntityExistenceDetails> _workingUpdateEntities = new List<EntityExistenceDetails>();
            private readonly List<EntityUpdateInputAction> _workingUpdateEntityInputs = new List<EntityUpdateInputAction>();

            public readonly FastList<ServerPendingPlayer> PendingPlayers;
            public readonly FastList<ServerActivePlayer> ActivePlayers;

            public ServerPlayerManager(int initialCapacity, NetworkEntityProcessor networkEntityProcessor)
            {
                PendingPlayers = new FastList<ServerPendingPlayer>(initialCapacity);
                ActivePlayers = new FastList<ServerActivePlayer>(initialCapacity);

                _networkEntityProcessor = networkEntityProcessor;
            }

            internal void UpdatePendingPlayerStates(NetworkMessageWriter networkMessageWriter)
            {
                var networkAssetDatabase = _networkEntityProcessor._networkAssetDatabase;
                var assetDefinitions = _networkEntityProcessor._assetDefinitions;

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
                var assetDefinitions = _networkEntityProcessor._assetDefinitions;
                var content = _networkEntityProcessor._content;
                var gameClockManager = _networkEntityProcessor._gameClockManager;

                var entityExistenceStates = _networkEntityProcessor._entityExistenceStates;

                var gameplayScene = _networkEntityProcessor._lazyLoadedScene.GetGameplayScene();

                var simulationTickNumber = gameClockManager.SimulationClock.SimulationTickNumber;
                // Can add to the scene now
                var playerPrefab = content.Load(assetDefinitions.ServerPlayer);
                var playerEntity = playerPrefab.InstantiateSingle();
                var networkEntityComp = playerEntity.Get<NetworkEntityComponent>();
                networkEntityComp.NetworkEntityId = pendingPlayer.PlayerId;       // Can just use the same ID
                networkEntityComp.OwnerType = NetworkOwnerType.Player;
                networkEntityComp.OwnerClientId = pendingPlayer.PlayerId;
                networkEntityComp.AssetId = assetDefinitions.ServerPlayerAssetId;
                networkEntityComp.IsLocalEntity = false;

                var networkPlayerComp = playerEntity.Get<NetworkPlayerComponent>();
                networkPlayerComp.PlayerName = pendingPlayer.PlayerName;

                var spawnPointEntity = gameplayScene.Entities.First(x => x.Name == "SpawnPoint");
                playerEntity.Transform.Position = spawnPointEntity.Transform.Position;
                playerEntity.GetChild(0).Transform.Rotation = spawnPointEntity.Transform.Rotation;  // Only rotate the child model entity

                _networkEntityProcessor.AddAndRegisterEntity(playerEntity, gameplayScene, simulationTickNumber);

                var newPlayer = new ServerActivePlayer(pendingPlayer.PlayerId, pendingPlayer.PlayerName, playerEntity, pendingPlayer.Connection);
                ActivePlayers.Add(newPlayer);
                // Notify the new player of all existing players
                for (int i = 0; i < ActivePlayers.Count - 1; i++)       // Exclude the last because that's the new player
                {
                    ref var existingPlayer = ref ActivePlayers.Items[i];
                    var existingPlayerDetails = entityExistenceStates[existingPlayer.PlayerEntity];
                    if (ActivePlayers.Items[i].PlayerId == newPlayer.PlayerId)
                    {
                    }
                    else
                    {
                        var spawnPlayer = new SpawnRemotePlayerMessage
                        {
                            PlayerId = existingPlayer.PlayerId,
                            SimulationTickNumber = existingPlayerDetails.SimulationTickNumberCreated,
                            PlayerName = existingPlayer.PlayerName,
                            Position = existingPlayer.PlayerEntity.Transform.Position,
                            Rotation = existingPlayer.PlayerEntity.GetChild(0).Transform.Rotation   // Rotation is from the child model entity
                        };
                        networkMessageWriter.Reset();
                        spawnPlayer.WriteTo(networkMessageWriter);
                        var conn = newPlayer.Connection;
                        conn.Send(networkMessageWriter, SendNetworkMessageType.ReliableOrdered);   // Use Ordered to ensure a player's joined & dropped events are in sequence
                    }
                }
                // Notify the new player of itself
                {
                    var spawnPlayer = new SpawnLocalPlayerMessage
                    {
                        PlayerId = newPlayer.PlayerId,
                        SimulationTickNumber = simulationTickNumber,
                        PlayerName = newPlayer.PlayerName,
                        Position = newPlayer.PlayerEntity.Transform.Position,
                        Rotation = newPlayer.PlayerEntity.GetChild(0).Transform.Rotation   // Rotation is from the child model entity
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
                        SimulationTickNumber = simulationTickNumber,
                        PlayerName = newPlayer.PlayerName,
                        Position = newPlayer.PlayerEntity.Transform.Position,
                        Rotation = newPlayer.PlayerEntity.GetChild(0).Transform.Rotation   // Rotation is from the child model entity
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
                            updateTransformMsg.Rotation = movementSnapshotStore.LocalRotation;
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

        /// <summary>
        /// Manager for all players when this game is a client.
        /// </summary>
        private class ClientPlayerManager
        {
            private NetworkEntityProcessor _networkEntityProcessor;
            private readonly FastList<LocalPlayerDetails> _localPlayers = new FastList<LocalPlayerDetails>();
            // Working list to hold entities to pass to MovementSnapshotsInputProcessor
            private readonly FastList<MovementSnapshotsInputProcessor.PredictMovementEntityData> _resimulateEntities = new FastList<MovementSnapshotsInputProcessor.PredictMovementEntityData>();

            public ClientPlayerManager(NetworkEntityProcessor networkEntityProcessor)
            {
                _networkEntityProcessor = networkEntityProcessor;
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
                    movementData.SnapshotType = SnapshotType.Server;
                    movementData.LocalPosition = updateTransform.Position;
                    movementData.LocalRotation = updateTransform.Rotation;
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
                    //if (!networkEntityComp.IsLocalEntity && movementData.MoveSpeedDecimalPercentage != 0)
                    //{
                    //    _networkEntityProcessor.DebugWriteLine($"UpdateEnt Sim {svrSimTickNumber} - Id {updateTransform.NetworkEntityId} - PIDApplied {movementData.PlayerInputSequenceNumberApplied} - MvDir {movementData.MoveDirection} - IsGrounded {movementData.IsGrounded} - Vel {movementData.PhysicsEngineLinearVelocity} - SpdPec {movementData.MoveSpeedDecimalPercentage}");
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

                    if (networkEntityComp.IsLocalEntity && data.ClientPredictionSnapshotsComponent != null)
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
                        if (!exists) {
                            _resimulateEntities.Add(new MovementSnapshotsInputProcessor.PredictMovementEntityData(
                                data.TransformComponent,
                                data.Entity.Get<Stride.Physics.CharacterComponent>(),
                                data.Entity.GetChild(0),
                                data.Entity.Get<InputSnapshotsComponent>(),
                                movementSnapshotsComp,
                                data.ClientPredictionSnapshotsComponent
                            ));
                        }
                    }
                }

                for (int i = 0; i < updateEntityInputs.Count; i++)
                {
                    ref var updateInput = ref updateEntityInputs.Items[i];
                    var data = networkEntityIdToEntityDataMap[updateInput.NetworkEntityId];
                    var networkEntityComp = data.NetworkEntityComponent;
                    if (networkEntityComp.LastAcknowledgedServerSimulationTickNumber >= updateInput.SimulationTickNumber)
                    {
                        continue;   // Redundant message, already added
                    }
                    switch (updateInput.InputActionType)
                    {
                        case InputActionType.Jump:
                            // TODO: update animation
                            break;
                    }

                    //entity.Get<CharacterComponent>();
                }

                if (_resimulateEntities.Count > 0)
                {
                    var movementSnapshotsInputProcessor = _networkEntityProcessor._lazyLoadedScene.GetMovementSnapshotsInputProcessor();
                    movementSnapshotsInputProcessor.Resimulate(currentSimulationTickNumber, _resimulateEntities);
                    _resimulateEntities.Clear();
                }
                ////foreach (var entity in _resimulateEntities)
                ////{
                ////    var movementSnapshotsComp = entity.Get<MovementSnapshotsComponent>();
                ////    if (movementSnapshotsComp.ServerToClientPredictionPositionDifference.LengthSquared() > 0)
                ////    {
                ////        // Resimulation required
                ////        var inputSnapshotsComp = entity.Get<InputSnapshotsComponent>();

                ////    }
                ////}
            }

            internal void SpawnLocalPlayer(NetworkMessageReader message, Scene gameplayScene, NetworkConnection connectionToServer)
            {
                SpawnLocalPlayerMessage msg = default;
                if (msg.TryRead(message))
                {
                    var simTickNumber = msg.SimulationTickNumber;
                    var playerEntity = AddPlayer(gameplayScene, simTickNumber, msg.PlayerId, msg.PlayerName, ref msg.Position, ref msg.Rotation, isLocalEntity: true);

                    var localPlayer = new LocalPlayerDetails(msg.PlayerId, playerEntity, connectionToServer);
                    Debug.Assert(!_localPlayers.Exists(x => x.PlayerId == msg.PlayerId));
                    _localPlayers.Add(localPlayer);
                }
            }

            internal void SpawnRemotePlayer(NetworkMessageReader message, Scene gameplayScene)
            {
                SpawnRemotePlayerMessage msg = default;
                if (msg.TryRead(message))
                {
                    var simTickNumber = msg.SimulationTickNumber;
                    AddPlayer(gameplayScene, simTickNumber, msg.PlayerId, msg.PlayerName, ref msg.Position, ref msg.Rotation, isLocalEntity: false);
                }
            }

            private Entity AddPlayer(
                Scene gameplayScene,
                SimulationTickNumber simulationTickNumber,
                SerializableGuid playerId,
                string playerName,
                ref Vector3 position,
                ref Quaternion rotation,
                bool isLocalEntity
                )
            {
                var networkEntityIdToEntityDataMap = _networkEntityProcessor._networkEntityIdToEntityDataMap;
                var assetDefinitions = _networkEntityProcessor._assetDefinitions;
                var content = _networkEntityProcessor._content;
                var gameEngineContext = _networkEntityProcessor._gameEngineContext;

                Debug.Assert(!networkEntityIdToEntityDataMap.ContainsKey(playerId));

                var networkEntityId = playerId;
                var assetId = isLocalEntity ? assetDefinitions.LocalPlayerAssetId : assetDefinitions.RemotePlayerAssetId;
                var prefabUrl = isLocalEntity ? assetDefinitions.LocalPlayer : assetDefinitions.RemotePlayer;
                var prefab = content.Load(prefabUrl);
                var entity = prefab.InstantiateSingle();

                var networkPlayerComp = entity.Get<NetworkPlayerComponent>();
                networkPlayerComp.PlayerName = playerName;

                var networkEntityComp = entity.Get<NetworkEntityComponent>();
                networkEntityComp.NetworkEntityId = networkEntityId;
                networkEntityComp.OwnerType = NetworkOwnerType.Player;
                networkEntityComp.OwnerClientId = playerId;
                networkEntityComp.IsLocalEntity = isLocalEntity;
                networkEntityComp.AssetId = assetId;

                _networkEntityProcessor.AddAndRegisterEntity(entity, gameplayScene, simulationTickNumber);
                // Set initial position
                var data = networkEntityIdToEntityDataMap[networkEntityId];
                var movementSnapshotsComp = data.MovementSnapshotsComponent;
                Debug.Assert(movementSnapshotsComp != null);

                ref var movementData = ref movementSnapshotsComp.SnapshotStore.GetOrCreate(simulationTickNumber);

                movementData.SnapshotType = SnapshotType.Server;
                movementData.LocalPosition = position;
                movementData.LocalRotation = rotation;

                _networkEntityProcessor.PlayerAdded?.Invoke(entity);
                return entity;
            }

            //internal void UnregisterLocalPlayer(SerializableGuid playerId)
            //{
            //    Debug.Assert(_localPlayers.Exists(x => x.PlayerId == playerId));
            //    _localPlayers.RemoveAll(x => x.PlayerId == playerId);
            //}

            internal void DespawnRemotePlayer(NetworkMessageReader message, Scene gameplayScene)
            {
                DespawnRemotePlayerMessage msg = default;
                if (msg.TryRead(message))
                {
                    var networkEntityIdToEntityDataMap = _networkEntityProcessor._networkEntityIdToEntityDataMap;

                    if (networkEntityIdToEntityDataMap.TryGetValue(msg.PlayerId, out var data))
                    {
                        var entity = data.Entity;
                        _networkEntityProcessor.RemoveAndUnregisterEntity(msg.PlayerId, entity, gameplayScene);
                        _networkEntityProcessor.PlayerRemoved?.Invoke(entity);
                    }
                    else
                    {
                        Debug.Fail($"Remote Player was not spawned: {msg.PlayerId}");
                    }
                }
            }

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

        private class LazyLoadedSceneData
        {
            private readonly NetworkEntityProcessor _networkEntityProcessor;

            private Scene _gameplayScene;
            private MovementSnapshotsInputProcessor _movementSnapshotsInputProcessor;

            public LazyLoadedSceneData(NetworkEntityProcessor networkEntityProcessor)
            {
                _networkEntityProcessor = networkEntityProcessor;
            }

            /// <summary>
            /// The scene where we actually add/remove gameplay related entities.
            /// </summary>
            public Scene GetGameplayScene()
            {
                if (_gameplayScene != null)
                {
                    return _gameplayScene;
                }

                // Entities are added to the InGameScreen scene rather than the root scene
                var sceneManager = _networkEntityProcessor._sceneSystem.GetSceneManagerFromRootScene();
                Debug.Assert(sceneManager != null, $"SceneManager entity must contain {nameof(SceneManager)} component.");
                Debug.Assert(sceneManager.ActiveMainSceneHandler is InGameSceneHandler, "Must be in-game.");
                _gameplayScene = sceneManager.ActiveMainSceneHandler.Scene;
                return _gameplayScene;
            }

            public MovementSnapshotsInputProcessor GetMovementSnapshotsInputProcessor()
            {
                if (_movementSnapshotsInputProcessor != null)
                {
                    return _movementSnapshotsInputProcessor;
                }

                var sceneSystem = _networkEntityProcessor._sceneSystem;
                _movementSnapshotsInputProcessor = sceneSystem.SceneInstance.GetProcessor<MovementSnapshotsInputProcessor>();
                Debug.Assert(_movementSnapshotsInputProcessor != null, $"You cannot call this method yet because {nameof(MovementSnapshotsInputProcessor)} hasn't been created yet.");
                return _movementSnapshotsInputProcessor;
            }
        }
    }
}
