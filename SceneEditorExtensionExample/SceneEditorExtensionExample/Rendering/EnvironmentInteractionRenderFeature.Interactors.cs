using SceneEditorExtensionExample.WorldTerrain.EnvironmentInteractions;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SceneEditorExtensionExample.Rendering;

partial class EnvironmentInteractionRenderFeature : SubRenderFeature
{
    internal static readonly PropertyKey<List<EnvironmentInteractorComponent>> EnvironmentInteractorsKey
        = new("EnvironmentInteractorRenderFeature.EnvironmentInteractors", typeof(EnvironmentInteractionRenderFeature));

    private class Interactors : IEnvironmentInteractionProcessor
    {
        private EnvironmentInteractionRenderFeature _parent;

        private LogicalGroupReference _interactorsViewGroupKey;
        private InteractorsPerViewData _interactorsPerViewData;

        public Interactors(EnvironmentInteractionRenderFeature parent)
        {
            _parent = parent;
        }

        public void Initialize()
        {
            _interactorsViewGroupKey = ((RootEffectRenderFeature)_parent.RootRenderFeature).CreateViewLogicalGroup("EnvironmentInteractors");

            _interactorsPerViewData.EnvironmentInteractorDataList = new(capacity: 16);
        }

        public void Extract()
        {
            if (!_parent.Context.VisibilityGroup.Tags.TryGetValue(EnvironmentInteractorsKey, out var environmentInteractors))
            {
                return;
            }

            _interactorsPerViewData.EnvironmentInteractorDataList.Clear();
            for (int i = 0; i < environmentInteractors.Count; i++)
            {
                var interactorComp = environmentInteractors[i];
                if (!interactorComp.IsEnabled)
                {
                    continue;
                }
                var interactorData = new EnvironmentInteractorData
                {
                    Position = interactorComp.GroundPosition,
                    Radius = interactorComp.Radius,
                };
                _interactorsPerViewData.EnvironmentInteractorDataList.Add(interactorData);
            }

            if (_interactorsPerViewData.EnvironmentInteractorDataList.Count > _interactorsPerViewData.PreviousEnvironmentInteractorCount)
            {
                _interactorsPerViewData.EnvironmentInteractorDataBuffer = _parent.Context.GraphicsDevice.CreateShaderBuffer<EnvironmentInteractorData>(_interactorsPerViewData.EnvironmentInteractorDataList.Count);
            }
            _interactorsPerViewData.PreviousEnvironmentInteractorCount = _interactorsPerViewData.EnvironmentInteractorDataList.Count;
        }

        public unsafe void Prepare(RenderDrawContext context)
        {
            if (_interactorsPerViewData.EnvironmentInteractorDataList.Count > 0)
            {
                Debug.Assert(_interactorsPerViewData.EnvironmentInteractorDataBuffer is not null);
                var dataSpan = _interactorsPerViewData.EnvironmentInteractorDataList.Items.AsSpan(start: 0, length: _interactorsPerViewData.EnvironmentInteractorDataList.Count);
                _interactorsPerViewData.EnvironmentInteractorDataBuffer.SetData(context.CommandList, dataSpan);
            }

            foreach (var view in _parent.RenderSystem.Views)
            {
                var viewFeature = view.Features[_parent.RootRenderFeature.Index];

                _interactorsViewGroupKey = ((RootEffectRenderFeature)_parent.RootRenderFeature).CreateViewLogicalGroup("EnvironmentInteractors");

                // Update PerView
                foreach (var viewLayout in viewFeature.Layouts)
                {
                    var logicalGroup = viewLayout.GetLogicalGroup(_interactorsViewGroupKey);
                    if (logicalGroup.Hash == default)
                    {
                        continue;
                    }

                    var resourceGroup = viewLayout.Entries[view.Index].Resources;

                    var mappedCB = (InteractorsPerViewCBufferData*)(resourceGroup.ConstantBuffer.Data + logicalGroup.ConstantBufferOffset);
                    mappedCB->EnvironmentInteractorCount = _interactorsPerViewData.EnvironmentInteractorDataList.Count;

                    resourceGroup.DescriptorSet.SetShaderResourceView(logicalGroup.DescriptorSlotStart, _interactorsPerViewData.EnvironmentInteractorDataBuffer);
                }
            }
        }
    }

    private struct EnvironmentInteractorData
    {
        public Vector3 Position;
        public float Radius;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct InteractorsPerViewCBufferData
    {
        public int EnvironmentInteractorCount;
    }

    private struct InteractorsPerViewData
    {
        public FastListStruct<EnvironmentInteractorData> EnvironmentInteractorDataList;
        public Buffer<EnvironmentInteractorData> EnvironmentInteractorDataBuffer;
        public int PreviousEnvironmentInteractorCount;
    }
}
