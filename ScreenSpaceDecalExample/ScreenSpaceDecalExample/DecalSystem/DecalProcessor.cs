using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;

namespace ScreenSpaceDecalExample.DecalSystem
{
    class DecalProcessor : EntityProcessor<DecalComponent, DecalProcessor.AssociatedData>, IEntityComponentRenderProcessor
    {
        private GraphicsDevice _graphicsDevice;

        public VisibilityGroup VisibilityGroup { get; set; }

        protected override void OnSystemAdd()
        {
            _graphicsDevice = Services.GetSafeServiceAs<IGraphicsDeviceService>().GraphicsDevice;
        }

        protected override AssociatedData GenerateComponentData([NotNull] Entity entity, [NotNull] DecalComponent component)
        {
            return new AssociatedData
            {
                TransformComponent = entity.Transform,
            };
        }

        protected override bool IsAssociatedDataValid([NotNull] Entity entity, [NotNull] DecalComponent component, [NotNull] AssociatedData data)
        {
            return true;
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] DecalComponent component, [NotNull] AssociatedData data)
        {
            var model = new Model();
            // We create the cube via the *ProceduralModel class rather than GeometricPrimitive.*.New
            // because there are additional vertext data that gets created through this which are required by the Material
            var procCube = new CubeProceduralModel();
            // Set up a new material with our decal shader.
            var shader = new ComputeShaderClassColor
            {
                MixinReference = "DecalShader"      // This is referring to our shader at Effects\DecalShader.xksl
            };

            var materialDescription = new MaterialDescriptor
            {
                Attributes =
                {
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Diffuse = new MaterialDiffuseMapFeature(shader),
                    Transparency = new MaterialTransparencyBlendFeature(),
                    CullMode = CullMode.Back,
                }
            };
            procCube.MaterialInstance.IsShadowCaster = false;
            procCube.Generate(Services, model);

            var material = Material.New(_graphicsDevice, materialDescription);
            UpdateMaterialParameters(component, data.TransformComponent, material);
            data.Material = material;
            data.Model = model;
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] DecalComponent component, [NotNull] AssociatedData data)
        {
            if (data.RenderMesh != null)
            {
                // Unregister from render system
                VisibilityGroup.RenderObjects.Remove(data.RenderMesh);
            }
        }

        public override void Draw(RenderContext context)
        {
            Dispatcher.ForEach(ComponentDatas, entity =>
            {
                var data = entity.Value;
                var decalComp = entity.Key;

                if (!decalComp.Enabled)
                {
                    if (data.IsShowing)
                    {
                        data.IsShowing = false;
                        lock (VisibilityGroup.RenderObjects)
                        {
                            VisibilityGroup.RenderObjects.Remove(data.RenderMesh);
                        }
                    }
                    return;
                }

                CheckMeshes(decalComp, data);
                UpdateAssociatedData(decalComp, data);

                if (!data.IsShowing)
                {
                    data.IsShowing = true;
                    // Update and register with render system
                    lock (VisibilityGroup.RenderObjects)
                    {
                        VisibilityGroup.RenderObjects.Add(data.RenderMesh);
                    }
                }
            });
        }

        private void UpdateAssociatedData(DecalComponent decalComponent, AssociatedData data)
        {
            if (data.Model == null)
            {
                // Shouldn't be null...
                return;
            }

            var renderMesh = data.RenderMesh;
            renderMesh.Enabled = decalComponent.Enabled;
            renderMesh.RenderGroup = decalComponent.DecalRenderGroup;

            // Copy world matrix
            renderMesh.World = data.TransformComponent.WorldMatrix;
            //renderMesh.IsScalingNegative = nodeTransformations[nodeIndex].IsScalingNegative;
            // RenderMesh.BoundingBox is in world space, so need to update its box position (and also its scale/rotation).
            var worldBoundingBox = data.Model.BoundingBox;
            BoundingBox.Transform(ref worldBoundingBox, ref data.TransformComponent.WorldMatrix, out worldBoundingBox);
            renderMesh.BoundingBox = new BoundingBoxExt(worldBoundingBox);
            //renderMesh.BlendMatrices = meshInfo.BlendMatrices;

            UpdateMaterialParameters(decalComponent, data.TransformComponent, data.Material);
        }

        private void UpdateMaterial(RenderMesh renderMesh, MaterialPass materialPass, MaterialInstance modelMaterialInstance, DecalComponent decalComponent)
        {
            renderMesh.MaterialPass = materialPass;

            renderMesh.IsShadowCaster = false;
            if (modelMaterialInstance != null)
            {
                renderMesh.IsShadowCaster = renderMesh.IsShadowCaster && modelMaterialInstance.IsShadowCaster;
            }
        }

        private void CheckMeshes(DecalComponent decalComponent, AssociatedData data)
        {
            if (data.RenderMesh == null)
            {
                var model = data.Model;

                // Create render mesh
                var mesh = model.Meshes[0];
                var material = data.Material;

                var renderMesh = new RenderMesh
                {
                    Source = decalComponent,
                    RenderModel = new RenderModel
                    {
                        Model = model,
                        Meshes = new RenderMesh[1],     // Cyclic relationship... renderMesh is assigned in here below.
                        Materials = new[]
                        {
                            new RenderModel.MaterialInfo
                            {
                                Material = material,
                                MeshCount = 1,
                                MeshStartIndex = 0
                            }
                        }
                    },
                    Mesh = mesh,
                };
                renderMesh.RenderModel.Meshes[0] = renderMesh;

                // Update material
                UpdateMaterial(renderMesh, material.Passes[0], model.Materials.GetItemOrNull(0), decalComponent);

                data.RenderMesh = renderMesh;

                // Update before first add so that RenderGroup is properly set
                UpdateAssociatedData(decalComponent, data);
            }
        }

        private static void UpdateMaterialParameters(
            DecalComponent decalComponent,
            TransformComponent transformComponent,
            Material material)
        {
            var projectorUpDir = Vector3.UnitY;
            transformComponent.Rotation.Rotate(ref projectorUpDir);
            foreach (var pass in material.Passes)
            {
                pass.Parameters.Set(DecalShaderKeys.DecalTexture, decalComponent.DecalTexture);
                pass.Parameters.Set(DecalShaderKeys.TextureScale, decalComponent.DecalScale);
                pass.Parameters.Set(DecalShaderKeys.TintColor, decalComponent.Color);
                pass.Parameters.Set(DecalShaderKeys.IgnoreRenderGroups, (uint)decalComponent.IgnoreRenderGroups);
                pass.Parameters.Set(DecalShaderKeys.IsAffectedByShadow, decalComponent.IsAffectedByShadow);
                pass.Parameters.Set(DecalShaderKeys.ProjectorUpDirection, projectorUpDir);
            }
        }

        internal class AssociatedData
        {
            public bool IsShowing;
            public TransformComponent TransformComponent;
            public Model Model;
            public RenderMesh RenderMesh;
            public Material Material;
        }
    }
}
