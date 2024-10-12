using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using StrideBuffer = Stride.Graphics.Buffer;

namespace SceneEditorExtensionExample;

public struct ModelMeshData
{
    public List<Vector3> Positions;
    public List<Vector3> Normals;
    public List<int> Indices;

    public void Clear()
    {
        Positions.Clear();
        Normals.Clear();
        Indices.Clear();
    }
}

public struct ModelMeshTriangleData
{
    public Vector3 Pos0;
    public Vector3 Pos1;
    public Vector3 Pos2;
    public Vector3 Normal;
    public ModelComponent ModelComponent;

    public ModelMeshTriangleData(Vector3 pos0, Vector3 pos1, Vector3 pos2, Vector3 normal, ModelComponent modelComponent)
    {
        Pos0 = pos0;
        Pos1 = pos1;
        Pos2 = pos2;
        Normal = normal;
        ModelComponent = modelComponent;
    }
}

public static class ModelHelper
{
    internal static unsafe bool TryGetMeshData(Model model, IServiceRegistry services, ModelMeshData modelMeshDataOutput)
    {
        // This only deals with static data (ie. no skinning)

        var contentManager = services.GetService<ContentManager>();
        var game = services.GetService<IGame>();
        var graphicsContext = game.GraphicsContext;

        int totalVerts = 0, totalIndices = 0;
        foreach (var meshData in model.Meshes)
        {
            totalVerts += meshData.Draw.VertexBuffers[0].Count;
            totalIndices += meshData.Draw.IndexBuffer.Count;
        }

        var vertexPositions = modelMeshDataOutput.Positions;
        vertexPositions.EnsureCapacity(vertexPositions.Count + totalVerts);
        var vertexNormals = modelMeshDataOutput.Normals;
        vertexNormals.EnsureCapacity(vertexNormals.Count + totalVerts);
        var vertexIndices = modelMeshDataOutput.Indices;
        vertexIndices.EnsureCapacity(vertexIndices.Count + totalIndices);

        foreach (var meshData in model.Meshes)
        {
            var vertexDeclaration = meshData.Draw.VertexBuffers[0].Declaration;
            var offsetMapping = vertexDeclaration
                .EnumerateWithOffsets()
                .ToDictionary(x => x.VertexElement.SemanticAsText, x => x.Offset);

            int positionElementOffset = offsetMapping[VertexElementUsage.Position];
            //int uvOffset = offsetMapping[VertexElementUsage.TextureCoordinate];
            int normalElementOffset = offsetMapping.GetValueOrDefault(VertexElementUsage.Normal, defaultValue: -1);

            var vertexBuffer = meshData.Draw.VertexBuffers[0].Buffer;
            var indexBuffer = meshData.Draw.IndexBuffer.Buffer;
            bool isVertexBufferContentLoaded = TryFetchBufferContent(vertexBuffer, graphicsContext.CommandList, out byte[] verticesBytes);
            bool isIndexBufferContentLoaded = TryFetchBufferContent(indexBuffer, graphicsContext.CommandList, out byte[] indicesBytes);

            if (!isVertexBufferContentLoaded || verticesBytes.Length == 0)
            {
                //throw new InvalidOperationException($"Failed to load mesh vertex buffer.");
                return false;
            }
            if (!isIndexBufferContentLoaded || indicesBytes.Length == 0)
            {
                //throw new InvalidOperationException($"Failed to load mesh index buffer.");
                return false;
            }

            int vertMappingStart = vertexPositions.Count;

            fixed (byte* bytePtr = verticesBytes)
            {
                var vertBufferBinding = meshData.Draw.VertexBuffers[0];
                int count = vertBufferBinding.Count;
                int stride = vertBufferBinding.Declaration.VertexStride;
                int positionElementPtrOffset = vertBufferBinding.Offset + positionElementOffset;
                var curPosElemPtr = bytePtr + positionElementPtrOffset;
                for (int i = 0; i < count; i++)
                {
                    var pos = *(Vector3*)curPosElemPtr;
                    vertexPositions.Add(pos);

                    curPosElemPtr += stride;
                }
                if (normalElementOffset >= 0)
                {
                    int normalElementPtrOffset = vertBufferBinding.Offset + normalElementOffset;
                    var curNormElemPtr = bytePtr + normalElementPtrOffset;
                    for (int i = 0; i < count; i++)
                    {
                        var norm = *(Vector3*)curNormElemPtr;
                        vertexNormals.Add(norm);

                        curNormElemPtr += stride;
                    }
                }
            }

            fixed (byte* bytePtr = indicesBytes)
            {
                if (meshData.Draw.IndexBuffer.Is32Bit)
                {
                    foreach (int i in new Span<int>(bytePtr + meshData.Draw.IndexBuffer.Offset, meshData.Draw.IndexBuffer.Count))
                    {
                        vertexIndices.Add(vertMappingStart + i);
                    }
                }
                else
                {
                    foreach (ushort i in new Span<ushort>(bytePtr + meshData.Draw.IndexBuffer.Offset, meshData.Draw.IndexBuffer.Count))
                    {
                        vertexIndices.Add(vertMappingStart + i);
                    }
                }
            }

            if (normalElementOffset < 0)
            {
                const int VerticesPerTriangle = 3;
                for (int i = 0; i < vertexIndices.Count; i += VerticesPerTriangle)
                {
                    int idx0 = vertexIndices[i];
                    int idx1 = vertexIndices[i + 1];
                    int idx2 = vertexIndices[i + 2];
                    var pos0 = vertexPositions[idx0];
                    var pos1 = vertexPositions[idx1];
                    var pos2 = vertexPositions[idx2];

                    var vec1To0 = pos1 - pos0;
                    var vec2To0 = pos2 - pos0;
                    Vector3.Cross(ref vec1To0, ref vec2To0, out var normalVec);
                    normalVec.Normalize();

                    for (int j = 0; j < VerticesPerTriangle; j++)
                    {
                        vertexNormals.Add(normalVec);
                        vertexNormals.Add(normalVec);
                        vertexNormals.Add(normalVec);
                    }
                }
            }
        }

        return true;
    }

    private static unsafe bool TryFetchBufferContent(
        StrideBuffer buffer, CommandList commandList, out byte[] output)
    {
        // Code taken from Stride.Physics.StaticMeshColliderShape.TryFetchBufferContent

        output = new byte[buffer.SizeInBytes];
        fixed (byte* window = output)
        {
            var ptr = new DataPointer(window, output.Length);
            if (buffer.Description.Usage == GraphicsResourceUsage.Staging)
            {
                // Directly if this is a staging resource
                buffer.GetData(commandList, buffer, ptr);
            }
            else
            {
                // inefficient way to use the Copy method using dynamic staging texture
                using var throughStaging = buffer.ToStaging();
                buffer.GetData(commandList, throughStaging, ptr);
            }
        }

        return true;
    }
}
