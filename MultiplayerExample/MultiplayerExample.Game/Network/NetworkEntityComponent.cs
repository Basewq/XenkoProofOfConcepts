using MultiplayerExample.Core;
using Stride.Core;
using Stride.Engine;

namespace MultiplayerExample.Network
{
    [DataContract]
    public class NetworkEntityComponent : EntityComponent
    {
        /// <summary>
        /// The ID of the entity for referencing between client and server.
        /// </summary>
        internal SerializableGuid NetworkEntityId;

        public NetworkOwnerType OwnerType;

        /// <summary>
        /// The ID of the client who owns this entity, when OwnerType is <see cref="NetworkOwnerType.Player"/>.
        /// </summary>
        internal SerializableGuid OwnerClientId;

        internal SimulationTickNumber LastAcknowledgedServerSimulationTickNumber;
        /// <summary>
        /// The AssetId that generated this entity.
        /// Used by the server to tell the client (player) which prefab to load.
        /// </summary>
        internal SerializableGuid AssetId;

        public bool IsLocalEntity { get; set; }
    }
}
