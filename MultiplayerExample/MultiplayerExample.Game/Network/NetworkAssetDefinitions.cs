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

        [DataMember(100)]
        public UrlReference<Prefab> ServerPlayer;
        [DataMember(105)]
        public UrlReference<Prefab> LocalPlayer;
        [DataMember(110)]
        public UrlReference<Prefab> RemotePlayer;

        internal SerializableGuid ServerPlayerAssetId;
        internal SerializableGuid LocalPlayerAssetId;
        internal SerializableGuid RemotePlayerAssetId;

        internal void LoadAssetIds(NetworkAssetDatabase networkAssetDatabase)
        {
            ServerPlayerAssetId = networkAssetDatabase.GetAssetIdFromUrlReference(ServerPlayer);
            LocalPlayerAssetId = networkAssetDatabase.GetAssetIdFromUrlReference(LocalPlayer);
            RemotePlayerAssetId = networkAssetDatabase.GetAssetIdFromUrlReference(RemotePlayer);
        }
    }
}
