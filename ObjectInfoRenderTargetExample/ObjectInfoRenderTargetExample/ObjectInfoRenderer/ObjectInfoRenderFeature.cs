﻿using Xenko.Core;
using Xenko.Engine;
using Xenko.Rendering;

namespace ObjectInfoRenderTargetExample.ObjectInfoRenderer
{
    public class ObjectInfoRenderFeature : SubRenderFeature
    {
        // Make sure it's public so it is visible to Game Studio.
        // In Game Studio, open the Graphics Compositor asset -> Render features -> MeshRenderFeature
        // Add 'ObjectInfoRenderFeature ' to Render Features in the property grid of the MeshRenderFeature.

        private ObjectPropertyKey<ObjectInfoData> _objectInfoPropertyKey;

        private ConstantBufferOffsetReference _objectInfoDataBuffer;

#if DEBUG
        private bool _isFirstRun = true;
#endif

        protected override void InitializeCore()
        {
            _objectInfoPropertyKey = RootRenderFeature.RenderData.CreateObjectKey<ObjectInfoData>();

            _objectInfoDataBuffer = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(ObjectInfoOutputShaderKeys.ObjectInfoData.Name);
        }

        public override void Extract()
        {
            var objectInfoDataHolder = RootRenderFeature.RenderData.GetData(_objectInfoPropertyKey);

            foreach (var objectNodeReference in RootRenderFeature.ObjectNodeReferences)
            {
                var objectNode = RootRenderFeature.GetObjectNode(objectNodeReference);
                if (!(objectNode.RenderObject is RenderMesh renderMesh))
                {
                    continue;
                }
                int meshIndex = 0;
                if (!(renderMesh.Source is ModelComponent modelComponent))
                {
                    continue;
                }

                for (int i = 0; i < modelComponent.Model.Meshes.Count; i++)
                {
                    if (modelComponent.Model.Meshes[i] == renderMesh.Mesh)
                    {
                        meshIndex = i;
                        break;
                    }
                }

                var modelCompId = RuntimeIdHelper.ToRuntimeId(modelComponent);
                var objectInfoData = new ObjectInfoData(modelCompId, meshIndex, renderMesh.Mesh.MaterialIndex);
                objectInfoDataHolder[objectNodeReference] = objectInfoData;

#if DEBUG
                if (_isFirstRun)
                {
                    System.Diagnostics.Debug.WriteLine($"Entity: {modelComponent.Entity.Name} - modelCompId: {objectInfoData.ModelComponentId} - mesh/mat Index: {objectInfoData.MeshIndexAndMaterialIndex}");
                }
#endif
            }
#if DEBUG
            _isFirstRun = false;
#endif
        }

        public unsafe override void Prepare(RenderDrawContext context)
        {
            var objectInfoDataHolder = RootRenderFeature.RenderData.GetData(_objectInfoPropertyKey);

            foreach (var renderNode in ((RootEffectRenderFeature)RootRenderFeature).RenderNodes)
            {
                var perDrawLayout = renderNode.RenderEffect.Reflection?.PerDrawLayout;
                if (perDrawLayout == null)
                {
                    continue;
                }

                int objectInfoDataOffset = perDrawLayout.GetConstantBufferOffset(_objectInfoDataBuffer);
                if (objectInfoDataOffset == -1)
                {
                    continue;
                }

                var objectInfoData = objectInfoDataHolder[renderNode.RenderObject.ObjectNode];

                var mappedCB = renderNode.Resources.ConstantBuffer.Data;
                var objectInfoDataPtr = (ObjectInfoData*)((byte*)mappedCB + objectInfoDataOffset);

                *objectInfoDataPtr = objectInfoData;
            }
        }
    }
}
