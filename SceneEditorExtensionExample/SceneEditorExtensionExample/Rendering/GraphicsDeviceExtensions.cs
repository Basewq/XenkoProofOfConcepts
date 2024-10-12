using Stride.Graphics;
using System;
using System.Runtime.CompilerServices;
using Buffer = Stride.Graphics.Buffer;

namespace SceneEditorExtensionExample.Rendering;

internal static class GraphicsDeviceExtensions
{
    public static Buffer<T> CreateShaderBuffer<T>(this GraphicsDevice graphicsDevice, int elementCount)
         where T : unmanaged
    {
        return Buffer.New<T>(graphicsDevice, elementCount, BufferFlags.ShaderResource | BufferFlags.StructuredBuffer, GraphicsResourceUsage.Dynamic);
    }
}
