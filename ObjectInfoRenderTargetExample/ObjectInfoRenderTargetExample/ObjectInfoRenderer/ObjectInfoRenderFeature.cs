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

            _objectInfoDataBuffer = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(ObjectInfoOutputShaderKeys.ObjectInfo.Name);
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

                // RuntimeIdHelper.ToRuntimeId is how Xenko does it for its 'Picking' scene.
                // We should probably change this to use something more appropriate for our data.
                var modelCompId = RuntimeIdHelper.ToRuntimeId(modelComponent);
                var objectInfoData = new ObjectInfoData((uint)modelCompId, (ushort)meshIndex, (ushort)renderMesh.Mesh.MaterialIndex);
                objectInfoDataHolder[objectNodeReference] = objectInfoData;

#if DEBUG
                // This is only for debugging purposes, it can be removed.
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
            // This entire method shows how we pass the ObjectInfoData to the shader, and is exactly
            // how Xenko does it when it needs to pass data. Unfortunately, it is this code
            // that forces the project to require 'unsafe' code.
            var objectInfoDataHolder = RootRenderFeature.RenderData.GetData(_objectInfoPropertyKey);

            foreach (var renderNode in ((RootEffectRenderFeature)RootRenderFeature).RenderNodes)
            {
                // PerDrawLayout means we access this data in the shader via
                // cbuffer PerDraw { ... } (see ObjectInfoOutputShader.xlsl)
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