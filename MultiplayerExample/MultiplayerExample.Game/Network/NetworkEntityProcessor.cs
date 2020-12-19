using MultiplayerExample.Core;
using MultiplayerExample.Engine;
using MultiplayerExample.GameServices;
using MultiplayerExample.Network.NetworkMessages;
using MultiplayerExample.Network.SnapshotStores;
using MultiplayerExample.Utilities;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Games;
using Stride.Physics;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MultiplayerExample.Network
{
    partial class NetworkEntityProcessor : EntityProcessor<NetworkEntityComponent, NetworkEntityProcessor.AssociatedData>,
        INetworkPreUpdateProcessor, INetworkPostUpdateProcessor
    {
        private readonly ServerPlayerManager _serverPlayerManager;
        private readonly ClientPlayerManager _clientPlayerManager;

        private readonly Dictionary<SerializableGuid, AssociatedData> _networkEntityIdToEntityDataMap = new Dictionary<SerializableGuid, AssociatedData>();
        private readonly Dictionary<Entity, EntityExistenceDetails> _entityExistenceStates = new Dictionary<Entity, EntityExistenceDetails>();

        private GameClockManager _gameClockManager;

        private IGameNetworkService _networkService;
        private NetworkMessageWriter _networkMessageWriter;

        private ContentManager _content;
        private NetworkAssetDatabase _networkAssetDatabase;

        private LazyLoadedSceneData _lazyLoadedScene;

        public bool IsEnabled { get; set; }

        public NetworkEntityProcessor()
        {
            _networkMessageWriter = new NetworkMessageWriter(new LiteNetLib.Utils.NetDataWriter());
            _serverPlayerManager = new ServerPlayerManager(4, this);
            _clientPlayerManager = new ClientPlayerManager(this);

            Order = -20000;         // Ensure this occurs before other processors
            // Not using Enabled property, because that completely disables the processor, where it doesn't even pick up newly added entities
            IsEnabled = true;
        }

        protected override void OnSystemAdd()
        {
            _gameClockManager = Services.GetService<GameClockManager>();

            _networkService = Services.GetSafeServiceAs<IGameNetworkService>();

            _content = Services.GetSafeServiceAs<ContentManager>();
            _networkAssetDatabase = Services.GetSafeServiceAs<NetworkAssetDatabase>();

            var sceneSystem = Services.GetSafeServiceAs<SceneSystem>();
            _lazyLoadedScene = new LazyLoadedSceneData(sceneSystem);
        }

        protected override void OnSystemRemove()
        {
            _lazyLoadedScene.Dispose();
            _lazyLoadedScene = null;
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
                ClientPredictionSnapshotsComponent = entity.Get<ClientPredictionSnapshotsComponent>(),
                CharacterComponent = entity.Get<CharacterComponent>()
            };
        }

        protected override bool IsAssociatedDataValid([NotNull] Entity entity, [NotNull] NetworkEntityComponent component, [NotNull] AssociatedData associatedData)
        {
            return associatedData.TransformComponent == entity.Transform
                && associatedData.MovementSnapshotsComponent == entity.Get<MovementSnapshotsComponent>()
                && associatedData.NetworkEntityComponent == entity.Get<NetworkEntityComponent>()
                && associatedData.ClientPredictionSnapshotsComponent == entity.Get<ClientPredictionSnapshotsComponent>()
                && associatedData.CharacterComponent == entity.Get<CharacterComponent>();
        }

        internal ServerPlayerManager GetServerPlayerManager() => _serverPlayerManager;
        internal ClientPlayerManager GetClientPlayerManager() => _clientPlayerManager;

        public void PreUpdate(GameTime gameTime)
        {
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

        private Entity CreateAndAddClientPlayerEntity(
            SimulationTickNumber simulationTickNumber,
            SerializableGuid playerId,
            string playerName,
            ref Vector3 position,
            ref Quaternion rotation,
            bool isLocalEntity)
        {
            var gameplayScene = _lazyLoadedScene.GetGameplayScene();
            var assetDefinitions = _lazyLoadedScene.GetNetworkAssetDefinitions();

            Debug.Assert(!_networkEntityIdToEntityDataMap.ContainsKey(playerId));

            var networkEntityId = playerId;
            var prefabUrl = isLocalEntity ? assetDefinitions.PlayerAssets.ClientLocalPlayer : assetDefinitions.PlayerAssets.ClientRemotePlayer;
            var prefab = _content.Load(prefabUrl);
            var clientPlayerEntities = prefab.InstantiateClientPlayer();

            var playerEntity = clientPlayerEntities.PlayerEntity;
            var networkPlayerComp = playerEntity.Get<NetworkPlayerComponent>();
            networkPlayerComp.PlayerName = playerName;

            var networkEntityComp = playerEntity.Get<NetworkEntityComponent>();
            networkEntityComp.NetworkEntityId = networkEntityId;
            networkEntityComp.OwnerClientId = playerId;
            networkEntityComp.AssetId = _networkAssetDatabase.GetAssetIdFromUrlReference(prefabUrl);

            AddAndRegisterEntity(playerEntity, gameplayScene, simulationTickNumber);
            // Set initial position
            var data = _networkEntityIdToEntityDataMap[networkEntityId];
            var movementSnapshotsComp = data.MovementSnapshotsComponent;
            var characterComp = data.CharacterComponent;
            SetPlayerTransform(simulationTickNumber, movementSnapshotsComp, characterComp, ref position, ref rotation);

            // The 'viewable' player is added separately
            var playerViewEntity = clientPlayerEntities.PlayerViewEntity;
            gameplayScene.Entities.Add(playerViewEntity);

            return playerEntity;
        }

        private static void SetPlayerTransform(
            SimulationTickNumber simulationTickNumber,
            MovementSnapshotsComponent movementSnapshotsComponent,
            CharacterComponent characterComponent,
            ref Vector3 position,
            ref Quaternion rotation)
        {
            Debug.Assert(movementSnapshotsComponent != null);
            ref var movementData = ref movementSnapshotsComponent.SnapshotStore.GetOrCreate(simulationTickNumber);
            movementData.LocalPosition = position;
            movementData.SetRotationFromQuaternion(rotation);

            var transformComp = movementSnapshotsComponent.Entity.Transform;
            transformComp.Position = position;
            transformComp.UpdateWorldMatrix();
            characterComponent?.UpdatePhysicsTransformation();
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

            Debug.Assert(ComponentDatas.ContainsKey(existenceDetails.NetworkEntityComponent));
            var data = ComponentDatas[existenceDetails.NetworkEntityComponent];
            _networkEntityIdToEntityDataMap.Add(existenceDetails.NetworkEntityComponent.NetworkEntityId, data);
        }

        private void RemoveAndUnregisterEntity(SerializableGuid playerId, Entity entity, Scene gameplayScene)
        {
            bool isRemovedFromEntityIdToEntityDataMap = _networkEntityIdToEntityDataMap.Remove(playerId);
            bool isRemovedFromExistanceStates = _entityExistenceStates.Remove(entity);
            bool isRemovedFromScene = gameplayScene.Entities.Remove(entity);
        }

        [Conditional("DEBUG")]
        private void DebugWriteLine(string message)
        {
            Debug.WriteLine(message);
            Console.WriteLine(message);
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

        internal class AssociatedData
        {
            public Entity Entity;
            public TransformComponent TransformComponent;
            public NetworkEntityComponent NetworkEntityComponent;
            public MovementSnapshotsComponent MovementSnapshotsComponent;
            // Optional components
            public ClientPredictionSnapshotsComponent ClientPredictionSnapshotsComponent;
            public CharacterComponent CharacterComponent;
        }
    }
}
