using Stride.Core;
using Stride.Core.Serialization;
using Stride.Engine;

namespace MultiplayerExample.Network
{
    /**
     * This is attached to the root GameManager entity.
     * It is a workaround since Stride doesn't have scriptable objects.
     */
    [DataContract]
    public class NetworkAssetDefinitions : EntityComponent
    {
        [DataMember(10)]
        public UrlReference<Scene> InGameScene;

        [DataMember(10000)]
        public PlayerAssetDefinition PlayerAssets;
    }

    /// <summary>
    /// Thin wrapper struct to organize all player related assets.
    /// </summary>
    [DataContract]
    public struct PlayerAssetDefinition
    {
        /// <summary>
        /// The player entity to load on the server.
        /// </summary>
        [DataMember(00)]
        public UrlReference<Prefab> ServerRemotePlayer;
        /// <summary>
        /// The local player entity to load on the client.
        /// </summary>
        [DataMember(05)]
        public UrlReference<Prefab> ClientLocalPlayer;
        /// <summary>
        /// The remote player entity to load on the client.
        /// </summary>
        [DataMember(10)]
        public UrlReference<Prefab> ClientRemotePlayer;
    }
}
