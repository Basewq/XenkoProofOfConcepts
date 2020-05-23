using BepuPhysicsExample.BepuPhysicsIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VHACDSharp;
using Xenko.Assets.Textures;
using Xenko.Core;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.BuildEngine;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Graphics.Data;
using Xenko.Rendering;
using Buffer = Xenko.Graphics.Buffer;

namespace BepuPhysicsExample.GameStudioExt.Physics
{
    [AssetCompiler(typeof(BepuColliderShapeAsset), typeof(AssetCompilationContext))]
    internal class BepuColliderShapeAssetCompiler : AssetCompilerBase
    {
        static BepuColliderShapeAssetCompiler()
        {
            NativeLibrary.PreloadLibrary("VHACD.dll", typeof(BepuColliderShapeAssetCompiler));
        }

        public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
        {
            foreach (var type in AssetRegistry.GetAssetTypes(typeof(Model)))
            {
                yield return new BuildDependencyInfo(type, typeof(AssetCompilationContext), BuildDependencyType.CompileContent);
            }
            foreach (var type in AssetRegistry.GetAssetTypes(typeof(Skeleton)))
            {
                yield return new BuildDependencyInfo(type, typeof(AssetCompilationContext), BuildDependencyType.CompileContent);
            }
        }

        public override IEnumerable<Type> GetInputTypesToExclude(AssetItem assetItem)
        {
            foreach (var type in AssetRegistry.GetAssetTypes(typeof(Material)))
            {
                yield return type;
            }
            yield return typeof(TextureAsset);
        }

        public override IEnumerable<ObjectUrl> GetInputFiles(AssetItem assetItem)
        {
            var asset = (BepuColliderShapeAsset)assetItem.Asset;
            foreach (var desc in asset.ColliderShapes)
            {
                if (desc is BepuConvexHullColliderShapeDesc)
                {
                    var convexHullDesc = desc as BepuConvexHullColliderShapeDesc;

                    if (convexHullDesc.Model != null)
                    {
                        var url = AttachedReferenceManager.GetUrl(convexHullDesc.Model);

                        if (!string.IsNullOrEmpty(url))
                        {
                            yield return new ObjectUrl(UrlType.Content, url);
                        }
                    }
                }
            }
        }

        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (BepuColliderShapeAsset)assetItem.Asset;

            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(new ColliderShapeCombineCommand(targetUrlInStorage, asset, assetItem.Package) { InputFilesGetter = () => GetInputFiles(assetItem) });
        }

        public class ColliderShapeCombineCommand : AssetCommand<BepuColliderShapeAsset>
        {
            public ColliderShapeCombineCommand(string url, BepuColliderShapeAsset parameters, IAssetFinder assetFinder)
                : base(url, parameters, assetFinder)
            {
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);

                // Cloned list of collider shapes
                var descriptions = Parameters.ColliderShapes.ToList();

                var validShapes = Parameters.ColliderShapes.Where(x => x != null
                    && (x.GetType() != typeof(BepuConvexHullColliderShapeDesc) || ((BepuConvexHullColliderShapeDesc)x).Model != null)).ToList();

                // Pre process special types
                var hullColliders = (from shape in validShapes
                                     let type = shape.GetType()
                                     where type == typeof(BepuConvexHullColliderShapeDesc)
                                     select shape)
                                     .Cast<BepuConvexHullColliderShapeDesc>();
                foreach (var convexHullDesc in hullColliders)
                {
                    // Clone the convex hull shape description so the fields that should not be serialized can be cleared (Model in this case)
                    var convexHullDescClone = new BepuConvexHullColliderShapeDesc
                    {
                        Scaling = convexHullDesc.Scaling,
                        LocalOffset = convexHullDesc.LocalOffset,
                        LocalRotation = convexHullDesc.LocalRotation,
                        Decomposition = convexHullDesc.Decomposition,
                    };

                    // Replace shape in final result with cloned description
                    int replaceIndex = descriptions.IndexOf(convexHullDesc);
                    descriptions[replaceIndex] = convexHullDescClone;

                    var loadSettings = new ContentManagerLoaderSettings
                    {
                        ContentFilter = ContentManagerLoaderSettings.NewContentFilterByType(typeof(Mesh), typeof(Skeleton))
                    };

                    var modelAsset = assetManager.Load<Model>(AttachedReferenceManager.GetUrl(convexHullDesc.Model), loadSettings);
                    if (modelAsset == null) continue;

                    convexHullDescClone.ConvexHulls = new List<List<List<Vector3>>>();
                    convexHullDescClone.ConvexHullsIndices = new List<List<List<uint>>>();

                    commandContext.Logger.Info("Processing Bepu convex hull generation, this might take a while...");

                    var nodeTransforms = new List<Matrix>();

                    // Pre-compute all node transforms, assuming nodes are ordered... see ModelViewHierarchyUpdater

                    if (modelAsset.Skeleton == null)
                    {
                        Matrix baseMatrix;
                        Matrix.Transformation(ref convexHullDescClone.Scaling, ref convexHullDescClone.LocalRotation, ref convexHullDescClone.LocalOffset, out baseMatrix);
                        nodeTransforms.Add(baseMatrix);
                    }
                    else
                    {
                        var nodesLength = modelAsset.Skeleton.Nodes.Length;
                        for (int i = 0; i < nodesLength; i++)
                        {
                            Matrix localMatrix;
                            Matrix.Transformation(
                                ref modelAsset.Skeleton.Nodes[i].Transform.Scale,
                                ref modelAsset.Skeleton.Nodes[i].Transform.Rotation,
                                ref modelAsset.Skeleton.Nodes[i].Transform.Position, out localMatrix);

                            Matrix worldMatrix;
                            if (modelAsset.Skeleton.Nodes[i].ParentIndex != -1)
                            {
                                var nodeTransform = nodeTransforms[modelAsset.Skeleton.Nodes[i].ParentIndex];
                                Matrix.Multiply(ref localMatrix, ref nodeTransform, out worldMatrix);
                            }
                            else
                            {
                                worldMatrix = localMatrix;
                            }

                            if (i == 0)
                            {
                                Matrix baseMatrix;
                                Matrix.Transformation(ref convexHullDescClone.Scaling, ref convexHullDescClone.LocalRotation, ref convexHullDescClone.LocalOffset, out baseMatrix);
                                nodeTransforms.Add(baseMatrix * worldMatrix);
                            }
                            else
                            {
                                nodeTransforms.Add(worldMatrix);
                            }
                        }
                    }

                    commandContext.Logger.Info($"Found {nodeTransforms.Count} Node Transforms.");
                    for (int i = 0; i < nodeTransforms.Count; i++)
                    {
                        var i1 = i;
                        if (modelAsset.Meshes.All(x => x.NodeIndex != i1))
                        {
                            commandContext.Logger.Info("No geometry in this node.");
                            continue; // No geometry in the node
                        }

                        var combinedVerts = new List<float>();
                        var combinedIndices = new List<uint>();

                        var hullsList = new List<List<Vector3>>();
                        convexHullDescClone.ConvexHulls.Add(hullsList);

                        var indicesList = new List<List<uint>>();
                        convexHullDescClone.ConvexHullsIndices.Add(indicesList);

                        var nodeTransform = nodeTransforms[i];
                        foreach (var meshData in modelAsset.Meshes.Where(x => x.NodeIndex == i1))
                        {
                            var indexOffset = (uint)combinedVerts.Count / 3;

                            ref var vertexBufferBinding = ref meshData.Draw.VertexBuffers[0];
                            var stride = vertexBufferBinding.Declaration.VertexStride;

                            var vertexBufferRef = AttachedReferenceManager.GetAttachedReference(vertexBufferBinding.Buffer);
                            byte[] vertexData;
                            if (vertexBufferRef.Data != null)
                            {
                                vertexData = ((BufferData)vertexBufferRef.Data).Content;
                            }
                            else if (!string.IsNullOrEmpty(vertexBufferRef.Url))
                            {
                                var dataAsset = assetManager.Load<Buffer>(vertexBufferRef.Url);
                                vertexData = dataAsset.GetSerializationData().Content;
                            }
                            else
                            {
                                commandContext.Logger.Info("vertexBufferRef is empty in this node.");
                                continue;
                            }

                            var vertexIndex = vertexBufferBinding.Offset;
                            for (int v = 0; v < vertexBufferBinding.Count; v++)
                            {
                                var posMatrix = Matrix.Translation(
                                    new Vector3(BitConverter.ToSingle(vertexData, vertexIndex + 0),
                                    BitConverter.ToSingle(vertexData, vertexIndex + 4),
                                    BitConverter.ToSingle(vertexData, vertexIndex + 8)));

                                Matrix rotatedMatrix;
                                Matrix.Multiply(ref posMatrix, ref nodeTransform, out rotatedMatrix);

                                combinedVerts.Add(rotatedMatrix.TranslationVector.X);
                                combinedVerts.Add(rotatedMatrix.TranslationVector.Y);
                                combinedVerts.Add(rotatedMatrix.TranslationVector.Z);

                                vertexIndex += stride;
                            }

                            var indexBuffer = meshData.Draw.IndexBuffer;
                            var indexBufferRef = AttachedReferenceManager.GetAttachedReference(indexBuffer.Buffer);
                            byte[] indexData;
                            if (indexBufferRef.Data != null)
                            {
                                indexData = ((BufferData)indexBufferRef.Data).Content;
                            }
                            else if (!string.IsNullOrEmpty(indexBufferRef.Url))
                            {
                                var dataAsset = assetManager.Load<Buffer>(indexBufferRef.Url);
                                indexData = dataAsset.GetSerializationData().Content;
                            }
                            else
                            {
                                throw new Exception("Failed to find index buffer while building a convex hull.");
                            }

                            var indexIndex = indexBuffer.Offset;
                            for (int v = 0; v < indexBuffer.Count; v++)
                            {
                                if (indexBuffer.Is32Bit)
                                {
                                    combinedIndices.Add(BitConverter.ToUInt32(indexData, indexIndex) + indexOffset);
                                    indexIndex += 4;
                                }
                                else
                                {
                                    combinedIndices.Add(BitConverter.ToUInt16(indexData, indexIndex) + indexOffset);
                                    indexIndex += 2;
                                }
                            }
                        }

                        var decompositionDesc = new ConvexHullMesh.DecompositionDesc
                        {
                            VertexCount = (uint)combinedVerts.Count / 3,
                            IndicesCount = (uint)combinedIndices.Count,
                            Vertexes = combinedVerts.ToArray(),
                            Indices = combinedIndices.ToArray(),
                            Depth = convexHullDesc.Decomposition.Depth,
                            PosSampling = convexHullDesc.Decomposition.PosSampling,
                            PosRefine = convexHullDesc.Decomposition.PosRefine,
                            AngleSampling = convexHullDesc.Decomposition.AngleSampling,
                            AngleRefine = convexHullDesc.Decomposition.AngleRefine,
                            Alpha = convexHullDesc.Decomposition.Alpha,
                            Threshold = convexHullDesc.Decomposition.Threshold,
                            SimpleHull = !convexHullDesc.Decomposition.Enabled,
                        };

                        var convexHullMesh = new ConvexHullMesh();

                        convexHullMesh.Generate(decompositionDesc);

                        var count = convexHullMesh.Count;

                        commandContext.Logger.Info("Node generated " + count + " convex hulls.");

                        var vertexCountHull = 0;

                        for (uint h = 0; h < count; h++)
                        {
                            float[] points;
                            convexHullMesh.CopyPoints(h, out points);

                            var pointList = new List<Vector3>();

                            for (int v = 0; v < points.Length; v += 3)
                            {
                                var vert = new Vector3(points[v + 0], points[v + 1], points[v + 2]);
                                pointList.Add(vert);

                                vertexCountHull++;
                            }

                            hullsList.Add(pointList);

                            uint[] indices;
                            convexHullMesh.CopyIndices(h, out indices);

                            for (var t = 0; t < indices.Length; t += 3)
                            {
                                Utilities.Swap(ref indices[t], ref indices[t + 2]);
                            }

                            var indexList = new List<uint>(indices);

                            indicesList.Add(indexList);
                        }

                        convexHullMesh.Dispose();

                        commandContext.Logger.Info("For a total of " + vertexCountHull + " vertices.");
                    }
                }

                var runtimeShape = new BepuPhysicsColliderShape(descriptions);
                assetManager.Save(Url, runtimeShape);

                return Task.FromResult(ResultStatus.Successful);
            }
        }
    }
}
