using MultiplayerExample.Utilities;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace MultiplayerExample.Network
{
    /// <summary>
    /// Database that maps IDs to asset paths.
    /// </summary>
    class NetworkAssetDatabase
    {
        private readonly NetworkAssetMapping[] _assetMappings;
        private readonly Dictionary<SerializableGuid, int> _assetIdToMappingIndex;
        private readonly Dictionary<string, int> _assetPathToMappingIndex;

        public NetworkAssetDatabase(ContentManager contentManager, params string[] assetFolderUrls)
        {
            var map = new List<NetworkAssetMapping>();
            _assetIdToMappingIndex = new Dictionary<SerializableGuid, int>();
            _assetPathToMappingIndex = new Dictionary<string, int>();

            int indexOffset = 0;
            foreach (var url in assetFolderUrls)
            {
                var filePaths = contentManager.FileProvider.ListFiles(url, "*", VirtualSearchOption.AllDirectories);
                var assetIds = GenerateAssetIdsFromFilePaths(filePaths);
                for (int i = 0; i < filePaths.Length; i++)
                {
                    map.Add(new NetworkAssetMapping(assetIds[i], filePaths[i]));
                    int mapIndex = i + indexOffset;
                    Debug.Assert(!_assetIdToMappingIndex.ContainsKey(assetIds[i]), "Asset ID collision found. GenerateAssetIdsFromFilePaths should be changed to remove ID collision.");
                    _assetIdToMappingIndex.Add(assetIds[i], mapIndex);
                    _assetPathToMappingIndex.Add(filePaths[i], mapIndex);
                }
                indexOffset += filePaths.Length;
            }
            _assetMappings = map.ToArray();
        }

        public SerializableGuid GetAssetIdFromUrlReference<T>(T urlRef) where T : IUrlReference
        {
            var mapIndex = _assetPathToMappingIndex[urlRef.Url];
            var assetId = _assetMappings[mapIndex].AssetId;
            return assetId;
        }

        public SerializableGuid GetAssetIdFromPath(string filePath)
        {
            var mapIndex = _assetPathToMappingIndex[filePath];
            var assetId = _assetMappings[mapIndex].AssetId;
            return assetId;
        }

        public string GetPathFromAssetId(SerializableGuid assetId)
        {
            var mapIndex = _assetIdToMappingIndex[assetId];
            var filePath = _assetMappings[mapIndex].FilePath;
            return filePath;
        }

        public UrlReference<T> GetUrlReferenceFromAssetId<T>(SerializableGuid assetId) where T : class
        {
            var mapIndex = _assetIdToMappingIndex[assetId];
            var filePath = _assetMappings[mapIndex].FilePath;
            return new UrlReference<T>(filePath);
        }

        private static Guid[] GenerateAssetIdsFromFilePaths(string[] filePaths)
        {
            var assetIds = new Guid[filePaths.Length];
            const int GuidSizeInBytes = 16;
            using (var sha1 = new SHA1Managed())
            {
                for (int i = 0; i < filePaths.Length; i++)
                {
                    string filePath = filePaths[i];
                    using (var filePathBytesPool = ArrayPool<byte>.Shared.RentTemp(filePath.Length))
                    using (var guidBytesPool = ArrayPool<byte>.Shared.RentTemp(GuidSizeInBytes))
                    {
                        var filePathBytes = filePathBytesPool.Array;
                        Encoding.UTF8.GetBytes(filePath, charIndex: 0, charCount: filePath.Length, filePathBytes, byteIndex: 0);
                        var hashBytes = sha1.ComputeHash(filePathBytes, offset: 0, count: filePath.Length);

                        var guidBytes = guidBytesPool.Array;
                        for (int guidIdx = 0; guidIdx < GuidSizeInBytes; guidIdx++)
                        {
                            guidBytes[guidIdx] = hashBytes[guidIdx];        // For small arrays, this is faster than calling Array.Copy
                        }
                        // Should (hopefully) still be unique, despite truncating 160 bits to 128 bits.
                        // If not, consider salting the path.
                        var filePathGuid = new Guid(guidBytes);
                        assetIds[i] = filePathGuid;
                    }
                }
            }
            return assetIds;
        }
    }
}
