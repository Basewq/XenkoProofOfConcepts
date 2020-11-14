namespace MultiplayerExample.Network
{
    readonly struct NetworkAssetMapping
    {
        public readonly SerializableGuid AssetId;
        public readonly string FilePath;

        public NetworkAssetMapping(SerializableGuid assetId, string filePath)
        {
            AssetId = assetId;
            FilePath = filePath;
        }
    }
}
