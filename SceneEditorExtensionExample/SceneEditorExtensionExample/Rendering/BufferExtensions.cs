using Stride.Graphics;
using System;
using System.Runtime.CompilerServices;
using Buffer = Stride.Graphics.Buffer;

namespace SceneEditorExtensionExample.Rendering;

internal static class BufferExtensions
{
    public static unsafe void SetData<TData>(this Buffer<TData> buffer, CommandList commandList, Span<TData> fromData, int offsetInBytes = 0)
        where TData : unmanaged
    {
        fixed (void* from = fromData)
        {
            buffer.SetData(commandList, new DataPointer(from, fromData.Length * Unsafe.SizeOf<TData>()), offsetInBytes);
        }
    }
}
