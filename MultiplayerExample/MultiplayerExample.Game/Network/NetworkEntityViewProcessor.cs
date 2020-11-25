using MultiplayerExample.Engine;
using MultiplayerExample.GameServices;
using MultiplayerExample.Network.SnapshotStores;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MultiplayerExample.Network
{
    class NetworkEntityViewProcessor : EntityProcessor<NetworkEntityViewComponent, NetworkEntityViewProcessor.AssociatedData>
    {
        private readonly List<Entity> _workingRemovePlayerViewEntities = new List<Entity>();

        private GameClockManager _gameClockManager;

        private LazyLoadedSceneData _lazyLoadedScene;
        private GameManager _gameManager;
        private Scene _gameplayScene;

        public NetworkEntityViewProcessor()
        {
        }

        protected override void OnSystemAdd()
        {
            var gameEngineContext = Services.GetService<GameEngineContext>();
            Enabled = gameEngineContext.IsClient;

            _gameClockManager = Services.GetService<GameClockManager>();
            var sceneSystem = Services.GetSafeServiceAs<SceneSystem>();
            _lazyLoadedScene = new LazyLoadedSceneData(sceneSystem);

            // GameManager is expected to already be generated at this point since due to how Stride works,
            // NetworkEntityViewProcessor is created after an entity with NetworkEntityViewComponent is created,
            // which is after when the root scene has been created.
            _gameManager = _lazyLoadedScene.GetGameManager();
            Debug.Assert(_gameManager != null);
            //_gameManager.PlayerAdded += OnPlayerAdded;
            _gameManager.PlayerRemoved += OnPlayerRemoved;
        }

        protected override void OnSystemRemove()
        {
            //_gameManager.PlayerAdded -= OnPlayerAdded;
            _gameManager.PlayerRemoved -= OnPlayerRemoved;
            _gameManager = null;
        }

        //private void OnPlayerAdded(Entity playerEntity)
        //{
        //
        //}

        private void OnPlayerRemoved(Entity playerEntity)
        {
            foreach (var networkEntityViewComp in ComponentDatas.Keys)
            {
                if (networkEntityViewComp.NetworkedEntity == playerEntity)
                {
                    _workingRemovePlayerViewEntities.Add(networkEntityViewComp.Entity);
                }
            }
            _gameplayScene ??= _lazyLoadedScene.GetGameplayScene();
            foreach (var viewEnt in _workingRemovePlayerViewEntities)
            {
                bool wasRemoved = _gameplayScene.Entities.Remove(viewEnt);
                Debug.Assert(wasRemoved);
            }

            _workingRemovePlayerViewEntities.Clear();
        }

        protected override AssociatedData GenerateComponentData([NotNull] Entity entity, [NotNull] NetworkEntityViewComponent component)
        {
            return new AssociatedData
            {
                // Can also add other info/components here
                TransformComponent = entity.Transform,
            };
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] NetworkEntityViewComponent component, [NotNull] AssociatedData data)
        {
            var networkedEntity = component.NetworkedEntity;
            Debug.Assert(networkedEntity != null, $"{nameof(NetworkEntityViewComponent)} must reference another entity.");
            Debug.Assert(networkedEntity != entity, $"{nameof(NetworkEntityViewComponent)} cannot reference itself.");

            // Networked entity's components - Assume these never get reassigned.
            data.NetworkEntityComponent = networkedEntity.Get<NetworkEntityComponent>();
            data.MovementSnapshotsComponent = networkedEntity.Get<MovementSnapshotsComponent>();
            data.ClientPredictionSnapshotsComponent = networkedEntity.Get<ClientPredictionSnapshotsComponent>();
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] NetworkEntityViewComponent component, [NotNull] AssociatedData data)
        {
            data.TransformComponent = null;

            data.NetworkEntityComponent = null;
            data.MovementSnapshotsComponent = null;
            data.ClientPredictionSnapshotsComponent = null;
        }

        protected override bool IsAssociatedDataValid([NotNull] Entity entity, [NotNull] NetworkEntityViewComponent component, [NotNull] AssociatedData associatedData)
        {
            return associatedData.TransformComponent == entity.Transform;
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

            // Components on the Networked entity
            internal NetworkEntityComponent NetworkEntityComponent;
            internal MovementSnapshotsComponent MovementSnapshotsComponent;
            internal ClientPredictionSnapshotsComponent ClientPredictionSnapshotsComponent;   // Optional component
        }
    }
}
