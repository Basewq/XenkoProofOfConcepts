using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Rendering;
using System.Runtime.CompilerServices;

namespace ScreenSpaceDecalExample.ObjectInfoRenderer
{
    public class ObjectInfoRenderFeature : SubRenderFeature
    {
        // Make sure it's public so it is visible to Game Studio.
        // In Game Studio, open the Graphics Compositor asset -> Render features -> MeshRenderFeature
        // Add 'ObjectInfoRenderFeature' to Render Features in the property grid of the MeshRenderFeature.

        private ObjectPropertyKey<ObjectInfoData> _objectInfoPropertyKey;
        private ConstantBufferOffsetReference _objectInfoDataBuffer;

        private ObjectPropertyKey<Matrix[]> _blendMatricesPropertyKey;
        private ConstantBufferOffsetReference _blendMatricesDataBuffer;

#if DEBUG
        private bool _isFirstRun = true;
#endif

        protected override void InitializeCore()
        {
            _objectInfoPropertyKey = RootRenderFeature.RenderData.CreateObjectKey<ObjectInfoData>();
            _objectInfoDataBuffer = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(ObjectInfoOutputShaderKeys.ObjectInfo.Name);

            // Note that I am unsure why we need to create our own BlendMatrixArray key rather than
            // reuse TransformationSkinningKeys.BlendMatrixArray.
            // When I tried to reuse TransformationSkinningKeys.BlendMatrixArray, the skinned model had graphical glitches,
            // but creating our own key does not have this issue.
            _blendMatricesPropertyKey = RootRenderFeature.RenderData.CreateObjectKey<Matrix[]>();
            _blendMatricesDataBuffer = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(OioTransformationSkinningKeys.BlendMatrixArray.Name);
        }

        public override void Extract()
        {
            var objectInfoDataHolder = RootRenderFeature.RenderData.GetData(_objectInfoPropertyKey);
            var blendMatricesDataHolder = RootRenderFeature.RenderData.GetData(_blendMatricesPropertyKey);

            foreach (var objectNodeReference in RootRenderFeature.ObjectNodeReferences)
            {
                var objectNode = RootRenderFeature.GetObjectNode(objectNodeReference);
                if (objectNode.RenderObject is not RenderMesh renderMesh)
                {
                    continue;
                }

                blendMatricesDataHolder[objectNodeReference] = renderMesh.BlendMatrices;    // This is for our skinned models.

                if (renderMesh.Source is not ModelComponent modelComponent)
                {
                    continue;
                }

                var objectInfoData = new ObjectInfoData(modelComponent.RenderGroup);
                objectInfoDataHolder[objectNodeReference] = objectInfoData;

#if DEBUG
                // This is only for debugging purposes, it can be removed.
                if (_isFirstRun)
                {
                    System.Diagnostics.Debug.WriteLine($"Entity: {modelComponent.Entity.Name} - renderGrp: {objectInfoData.RenderGroup}");
                }
#endif
            }
#if DEBUG
            _isFirstRun = false;
#endif
        }

        public override void Prepare(RenderDrawContext context)
        {
            // This entire method shows how we pass the ObjectInfoData to the shader, and is similar to
            // how Stride does it when it needs to pass data.
            var objectInfoDataHolder = RootRenderFeature.RenderData.GetData(_objectInfoPropertyKey);
            Dispatcher.ForEach(((RootEffectRenderFeature)RootRenderFeature).RenderNodes, (ref RenderNode renderNode) =>
            {
                // PerDrawLayout means we access this data in the shader via
                // cbuffer PerDraw { ... } (see ObjectInfoOutputShader.sdsl)
                var perDrawLayout = renderNode.RenderEffect.Reflection?.PerDrawLayout;
                if (perDrawLayout == null)
                {
                    return;
                }

                int objectInfoDataOffset = perDrawLayout.GetConstantBufferOffset(_objectInfoDataBuffer);
                if (objectInfoDataOffset == -1)
                {
                    return;
                }

                // This is similar to TransformRenderFeature.Prepare
                unsafe
                {
                    ref var srcObjectInfoData = ref objectInfoDataHolder[renderNode.RenderObject.ObjectNode];
                    var mappedConstBufferIntPtr = renderNode.Resources.ConstantBuffer.Data;
                    var destPtr = (ObjectInfoData*)((byte*)mappedConstBufferIntPtr + objectInfoDataOffset);
                    *destPtr = srcObjectInfoData;
                }
            });

            // We can probably combine this with the above ForEach method, but separated here
            // for code clarity.
            var blendMatricesDataHolder = RootRenderFeature.RenderData.GetData(_blendMatricesPropertyKey);
            Dispatcher.ForEach(((RootEffectRenderFeature)RootRenderFeature).RenderNodes, (ref RenderNode renderNode) =>
            {
                var perDrawLayout = renderNode.RenderEffect.Reflection?.PerDrawLayout;
                if (perDrawLayout == null)
                {
                    return;
                }

                var blendMatricesOffset = perDrawLayout.GetConstantBufferOffset(_blendMatricesDataBuffer);
                if (blendMatricesOffset == -1)
                {
                    return;
                }

                var renderModelObjectInfo = blendMatricesDataHolder[renderNode.RenderObject.ObjectNode];
                if (renderModelObjectInfo == null)
                {
                    return;
                }

                // This is similar to SkinningRenderFeature.Prepare
                unsafe
                {
                    var mappedConstBufferIntPtr = renderNode.Resources.ConstantBuffer.Data;
                    uint arraySizeInBytes = (uint)(Matrix.SizeInBytes * renderModelObjectInfo.Length);
                    var destPtr = (byte*)mappedConstBufferIntPtr + blendMatricesOffset;
                    fixed (Matrix* srcObjectInfoPtr = renderModelObjectInfo)
                    {
                        Unsafe.CopyBlockUnaligned(destPtr, srcObjectInfoPtr, arraySizeInBytes);
                    }
                }
            });
        }
    }
}
