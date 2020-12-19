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
        private readonly List<Entity> _workingRemoveEntityViews = new List<Entity>();

        private GameClockManager _gameClockManager;

        private LazyLoadedSceneData _lazyLoadedScene;

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

            //EntityManager.EntityAdded += OnEntityAdded;
            EntityManager.EntityRemoved += OnEntityRemoved;
        }

        protected override void OnSystemRemove()
        {
            //EntityManager.EntityAdded -= OnEntityAdded;
            EntityManager.EntityRemoved -= OnEntityRemoved;

            _lazyLoadedScene.Dispose();
            _lazyLoadedScene = null;
        }

        //private void OnEntityAdded(object sender, Entity entity)
        //{
        //
        //}

        private void OnEntityRemoved(object sender, Entity entity)
        {
            if (entity.Get<NetworkEntityComponent>() == null)
            {
                // Not relevant to this processor
                return;
            }
            foreach (var networkEntityViewComp in ComponentDatas.Keys)
            {
                if (networkEntityViewComp.NetworkedEntity == entity)
                {
                    _workingRemoveEntityViews.Add(networkEntityViewComp.Entity);
                }
            }
            var gameplayScene = _lazyLoadedScene.GetGameplayScene();
            foreach (var viewEnt in _workingRemoveEntityViews)
            {
                bool wasRemoved = gameplayScene.Entities.Remove(viewEnt);
                Debug.Assert(wasRemoved);
            }

            _workingRemoveEntityViews.Clear();
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
