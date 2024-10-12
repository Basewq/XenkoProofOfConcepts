using SceneEditorExtensionExample.WorldTerrain.EnvironmentInteractions;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Rendering;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SceneEditorExtensionExample.Rendering;

public partial class EnvironmentInteractionRenderFeature : SubRenderFeature
{
    internal static readonly PropertyKey<List<EnvironmentWindSourceComponent>> EnvironmentWindSourcesKey
        = new("EnvironmentWindSourceRenderFeature.EnvironmentWindSources", typeof(EnvironmentInteractionRenderFeature));

    private class WindSources : IEnvironmentInteractionProcessor
    {
        private EnvironmentInteractionRenderFeature _parent;

        private LogicalGroupReference _windSourcesViewGroupKey;
        private WindSourcesPerViewData _windSourcesPerViewData;

        public WindSources(EnvironmentInteractionRenderFeature parent)
        {
            _parent = parent;
        }

        public void Initialize()
        {
            _windSourcesViewGroupKey = ((RootEffectRenderFeature)_parent.RootRenderFeature).CreateViewLogicalGroup("EnvironmentWindSources");

            _windSourcesPerViewData.WindDirectionalDataList = new(capacity: 2);
        }

        public void Extract()
        {
            if (!_parent.Context.VisibilityGroup.Tags.TryGetValue(EnvironmentWindSourcesKey, out var environmentWindSources))
            {
                return;
            }

            _windSourcesPerViewData.WindDirectionalDataList.Clear();
            for (int i = 0; i < environmentWindSources.Count; i++)
            {
                var envWindSourceComp = environmentWindSources[i];
                if (!envWindSourceComp.IsEnabled)
                {
                    continue;
                }
                Debug.Assert(envWindSourceComp.WindType is not null);
                envWindSourceComp.WindType.AddData(ref _windSourcesPerViewData);
            }
        }

        public unsafe void Prepare(RenderDrawContext context)
        {
            {   // Calculate the final directional wind
                var totalWindVelocity = Vector2.Zero;
                var actualWindVelocity = Vector2.Zero;
                for (int i = 0; i < _windSourcesPerViewData.WindDirectionalDataList.Count; i++)
                {
                    ref var windDir = ref _windSourcesPerViewData.WindDirectionalDataList.Items[i];
                    var windMaxVelocity = windDir.WindDirectionXZ * windDir.WindMaxSpeed;
                    totalWindVelocity += windMaxVelocity;
                    actualWindVelocity += windMaxVelocity * windDir.WindCurrentStrength;
                }

                float maxSpeed = totalWindVelocity.Length();
                float currentSpeed = actualWindVelocity.Length();
                float currentStrength = MathUtil.IsZero(maxSpeed) ? 0 : currentSpeed / maxSpeed;
                _windSourcesPerViewData.WindDirectionalFinal = new EnvironmentWindDirectionalData
                {
                    WindDirectionXZ = Vector2.Normalize(totalWindVelocity),
                    WindMaxSpeed = maxSpeed,
                    WindCurrentStrength = currentStrength
                };
                _windSourcesPerViewData.PreviousWindDirectionalCount = _windSourcesPerViewData.WindDirectionalDataList.Count;
            }

            foreach (var view in _parent.RenderSystem.Views)
            {
                var viewFeature = view.Features[_parent.RootRenderFeature.Index];

                _windSourcesViewGroupKey = ((RootEffectRenderFeature)_parent.RootRenderFeature).CreateViewLogicalGroup("EnvironmentWindSources");

                // Update PerView
                foreach (var viewLayout in viewFeature.Layouts)
                {
                    var logicalGroup = viewLayout.GetLogicalGroup(_windSourcesViewGroupKey);
                    if (logicalGroup.Hash == default)
                    {
                        continue;
                    }

                    var resourceGroup = viewLayout.Entries[view.Index].Resources;

                    var mappedCB = (WindSourcesPerViewCBufferData*)(resourceGroup.ConstantBuffer.Data + logicalGroup.ConstantBufferOffset);
                    mappedCB->WindAmbient = _windSourcesPerViewData.WindAmbient;
                    mappedCB->WindDirectional = _windSourcesPerViewData.WindDirectionalFinal;

                    //resourceGroup.DescriptorSet.SetShaderResourceView(logicalGroup.DescriptorSlotStart, _windSourcesPerViewData.WindDirectionalDataBuffer);
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindSourcesPerViewCBufferData
    {
        public EnvironmentWindAmbientData WindAmbient;
        public EnvironmentWindDirectionalData WindDirectional;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct EnvironmentWindAmbientData
{
    public float WindSpeed;
}

[StructLayout(LayoutKind.Sequential)]
public struct EnvironmentWindDirectionalData
{
    public Vector2 WindDirectionXZ;
    public float WindMaxSpeed;
    public float WindCurrentStrength;
}

public struct WindSourcesPerViewData
{
    public EnvironmentWindAmbientData WindAmbient;

    public FastListStruct<EnvironmentWindDirectionalData> WindDirectionalDataList;
    public int PreviousWindDirectionalCount;
    public EnvironmentWindDirectionalData WindDirectionalFinal;
    //public Buffer<EnvironmentWindDirectionalData> WindDirectionalDataBuffer;
}
