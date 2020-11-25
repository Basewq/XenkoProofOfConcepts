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
    /// <remarks>
    /// Note that we define our own IDs instead of using <see cref="ContentManager"/>'s FileProvider is because the ObjectIds
    /// are not guaranteed to be consistent between the client and server applications.
    /// </remarks>
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
                var contentPaths = contentManager.FileProvider.ListFiles(url, "*", VirtualSearchOption.AllDirectories);
                var assetIds = GenerateAssetIdsFromFilePaths(contentPaths);
                for (int i = 0; i < contentPaths.Length; i++)
                {
                    map.Add(new NetworkAssetMapping(assetIds[i], contentPaths[i]));
                    int mapIndex = i + indexOffset;
                    Debug.Assert(!_assetIdToMappingIndex.ContainsKey(assetIds[i]), "Asset ID collision found. GenerateAssetIdsFromFilePaths should be changed to remove ID collision.");
                    _assetIdToMappingIndex.Add(assetIds[i], mapIndex);
                    _assetPathToMappingIndex.Add(contentPaths[i], mapIndex);
                }
                indexOffset += contentPaths.Length;
            }
            _assetMappings = map.ToArray();
        }

        public SerializableGuid GetAssetIdFromUrlReference<T>(T urlRef) where T : IUrlReference
        {
            var mapIndex = _assetPathToMappingIndex[urlRef.Url];
            var assetId = _assetMappings[mapIndex].AssetId;
            return assetId;
        }

        public SerializableGuid GetAssetIdFromContentPath(string contentPath)
        {
            var mapIndex = _assetPathToMappingIndex[contentPath];
            var assetId = _assetMappings[mapIndex].AssetId;
            return assetId;
        }

        public string GetContentPathFromAssetId(SerializableGuid assetId)
        {
            var mapIndex = _assetIdToMappingIndex[assetId];
            var contentPath = _assetMappings[mapIndex].FilePath;
            return contentPath;
        }

        public UrlReference<T> GetUrlReferenceFromAssetId<T>(SerializableGuid assetId) where T : class
        {
            var mapIndex = _assetIdToMappingIndex[assetId];
            var contentPath = _assetMappings[mapIndex].FilePath;
            return new UrlReference<T>(contentPath);
        }

        private static Guid[] GenerateAssetIdsFromFilePaths(string[] contentPaths)
        {
            var assetIds = new Guid[contentPaths.Length];
            const int GuidSizeInBytes = 16;
            using (var sha1 = new SHA1Managed())
            {
                for (int i = 0; i < contentPaths.Length; i++)
                {
                    string contentPath = contentPaths[i];
                    using (var contentPathBytesPool = ArrayPool<byte>.Shared.RentTemp(contentPath.Length))
                    using (var guidBytesPool = ArrayPool<byte>.Shared.RentTemp(GuidSizeInBytes))
                    {
                        var contentPathBytes = contentPathBytesPool.Array;
                        Encoding.UTF8.GetBytes(contentPath, charIndex: 0, charCount: contentPath.Length, contentPathBytes, byteIndex: 0);
                        var hashBytes = sha1.ComputeHash(contentPathBytes, offset: 0, count: contentPath.Length);

                        var guidBytes = guidBytesPool.Array;
                        for (int guidIdx = 0; guidIdx < GuidSizeInBytes; guidIdx++)
                        {
                            guidBytes[guidIdx] = hashBytes[guidIdx];        // For small arrays, this is faster than calling Array.Copy
                        }
                        // Despite truncating 160 bits to 128 bits, the IDs should still be unique (hopefully).
                        // If not, consider salting the file path when hashing until it is all unique again.
                        var contentPathGuid = new Guid(guidBytes);
                        assetIds[i] = contentPathGuid;
                    }
                }
            }
            return assetIds;
        }
    }
}
