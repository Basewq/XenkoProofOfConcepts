using SceneEditorExtensionExample.Rendering;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Graphics;
using System;

namespace SceneEditorExtensionExample.WorldTerrain.Foliage;

/// <summary>
/// Additional instancing data specifically for the foliage instancing.
/// This is generated at run-time by <see cref="FoliageInstancingManagerComponent"/>.
/// </summary>
[DefaultEntityComponentRenderer(typeof(FoliageChunkInstancingProcessor))]
[Display(Browsable = false)]
internal class FoliageChunkInstancingComponent : EntityComponent, IDisposable
{
    private bool _isDisposed;

    public FoliageChunkId ChunkId;
    public ModelComponent ModelComponent;
    public InstancingUserArray InstancingArray;     // Same object as InstancingComponent.Type
    public Buffer<FoliageInstanceData>? InstanceDataBuffer;

    protected void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                InstanceDataBuffer?.Dispose();
            }

            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
