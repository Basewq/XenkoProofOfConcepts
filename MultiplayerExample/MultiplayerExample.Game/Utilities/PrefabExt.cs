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
    }
}
