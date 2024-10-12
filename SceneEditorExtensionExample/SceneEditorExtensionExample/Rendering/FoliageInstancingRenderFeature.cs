using SceneEditorExtensionExample.WorldTerrain.Foliage;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SceneEditorExtensionExample.Rendering;

[StructLayout(LayoutKind.Sequential)]
public struct FoliageInstanceData
{
    public Vector3 SurfaceNormalModelSpace;
}

// Code adapted from Stride.Rendering.InstancingRenderFeature
public partial class FoliageInstancingRenderFeature : SubRenderFeature
{
    internal static readonly PropertyKey<Dictionary<RenderModel, FoliageChunkInstancingComponent>> ModelToInstancingMapKey
        = new("FoliageInstancingRenderFeature.ModelToInstancingMap", typeof(FoliageInstancingRenderFeature));

    private StaticObjectPropertyKey<FoliageInstancingPerDrawData> _renderObjectInstancingDataKey;
    private LogicalGroupReference _instancingDrawGroupKey;

    /// <inheritdoc/>
    protected override void InitializeCore()
    {
        _renderObjectInstancingDataKey = RootRenderFeature.RenderData.CreateStaticObjectKey<FoliageInstancingPerDrawData>();
        _instancingDrawGroupKey = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawLogicalGroup("FoliageInstancing");
    }

    /// <inheritdoc/>
    public override void Extract()
    {
        if (Context.VisibilityGroup is null
            || !Context.VisibilityGroup.Tags.TryGetValue(ModelToInstancingMapKey, out var modelToInstancingMap))
        {
            return;
        }

        var renderObjectInstancingData = RootRenderFeature.RenderData.GetData(_renderObjectInstancingDataKey);

        foreach (var objectNodeReference in RootRenderFeature.ObjectNodeReferences)
        {
            var objectNode = RootRenderFeature.GetObjectNode(objectNodeReference);
            if (objectNode.RenderObject is not RenderMesh renderMesh)
            {
                continue;
            }

            var renderModel = renderMesh.RenderModel;
            if (renderModel is null)
            {
                continue;
            }

            if (!modelToInstancingMap.TryGetValue(renderModel, out var renderInstancing))
            {
                continue;
            }

            ref var instancingData = ref renderObjectInstancingData[renderMesh.StaticObjectNode];

            instancingData.InstanceCount = renderInstancing.InstancingArray.InstanceCount;
            instancingData.InstancingDataBuffer = renderInstancing.InstanceDataBuffer;
        }
    }

    /// <inheritdoc/>
    public unsafe override void Prepare(RenderDrawContext context)
    {
        var renderObjectInstancingData = RootRenderFeature.RenderData.GetData(_renderObjectInstancingDataKey);

        // Note we don't need to set the buffer data since FoliageInstancingManagerComponent has already done it.

        // Assign buffers to render node
        foreach (var renderNode in ((RootEffectRenderFeature)RootRenderFeature).RenderNodes)
        {
            var perDrawLayout = renderNode.RenderEffect.Reflection?.PerDrawLayout;
            if (perDrawLayout is null)
            {
                continue;
            }

            //if (instancingResourceGroupKey.Index < 0)
            //    continue;

            var group = perDrawLayout.GetLogicalGroup(_instancingDrawGroupKey);
            if (group.DescriptorEntryStart == -1)
            {
                continue;
            }

            if (renderNode.RenderObject is not RenderMesh renderMesh)
            {
                continue;
            }

            ref var instancingData = ref renderObjectInstancingData[renderMesh.StaticObjectNode];

            if (instancingData.InstanceCount > 0)
            {
                renderNode.Resources.DescriptorSet.SetShaderResourceView(group.DescriptorEntryStart, instancingData.InstancingDataBuffer);
            }
        }
    }

    private struct FoliageInstancingPerDrawData
    {
        public int InstanceCount;
        public Buffer<FoliageInstanceData> InstancingDataBuffer;
    }
}
