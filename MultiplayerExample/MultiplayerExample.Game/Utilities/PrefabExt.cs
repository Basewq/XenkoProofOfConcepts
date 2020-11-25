using MultiplayerExample.Network;
using Stride.Engine;
using Stride.Engine.Design;
using System.Diagnostics;

namespace MultiplayerExample.Utilities
{
    static class PrefabExt
    {
        /// <summary>
        /// Creates the entity from the prefab.
        /// </summary>
        public static Entity InstantiateSingle(this Prefab prefab)
        {
            Debug.Assert(prefab.Entities.Count == 1, "Prefab must only have one root entity.");
            var entity = EntityCloner.Clone(prefab.Entities[0]);
            return entity;
        }

        /// <summary>
        /// Creates the player entities from the supplied player prefab. This is only applicable for client side prefabs,
        /// since the 'view' entity is required.
        /// </summary>
        public static ClientPlayerEntities InstantiateClientPlayer(this Prefab playerPrefab)
        {
            Debug.Assert(playerPrefab.Entities.Count == 2, "Prefab must have have two entities (the player entity and the view entity).");
            var entities = playerPrefab.Instantiate();
            var playerEntity = entities.Find(x => x.Get<NetworkEntityComponent>() != null);
            var playerViewEntity = entities.Find(x => x.Get<NetworkEntityViewComponent>() != null);
            Debug.Assert(playerEntity != null, $"Prefab must contain an entity with {nameof(NetworkEntityComponent)}.");
            Debug.Assert(playerViewEntity != null, $"Prefab must contain an entity with {nameof(NetworkEntityViewComponent)}.");
            return new ClientPlayerEntities(playerEntity, playerViewEntity);
        }

        public readonly struct ClientPlayerEntities
        {
            public readonly Entity PlayerEntity;
            public readonly Entity PlayerViewEntity;

            public ClientPlayerEntities(Entity playerEntity, Entity playerViewEntity)
            {
                PlayerEntity = playerEntity;
                PlayerViewEntity = playerViewEntity;
            }
        }
    }
}
