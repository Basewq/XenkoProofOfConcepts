using ScreenSpaceDecalExample.DecalSystem.Renderer;
using Xenko.Core.Annotations;
using Xenko.Core.Threading;
using Xenko.Engine;
using Xenko.Rendering;

namespace ScreenSpaceDecalExample.DecalSystem
{
    class DecalProcessor : EntityProcessor<DecalComponent, RenderDecalData>, IEntityComponentRenderProcessor
    {
        public VisibilityGroup VisibilityGroup { get; set; }

        //public DecalProcessor() : base(typeof(ModelComponent))
        //{
        //}

        protected override RenderDecalData GenerateComponentData([NotNull] Entity entity, [NotNull] DecalComponent component)
        {
            return new RenderDecalData();
        }

        protected override bool IsAssociatedDataValid([NotNull] Entity entity, [NotNull] DecalComponent component, [NotNull] RenderDecalData data)
        {
            return true;
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] DecalComponent component, [NotNull] RenderDecalData data)
        {
            data.RenderObject = new DecalRenderObject();
            data.RenderObject.RenderGroup = component.RenderGroup;  // Must set this immediately, otherwise the rendering system won't pick it up.

            VisibilityGroup.RenderObjects.Add(data.RenderObject);
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] DecalComponent component, [NotNull] RenderDecalData data)
        {
            // Unregister from the Rendering System
            if (data.RenderObject != null)
            {
                VisibilityGroup.RenderObjects.Remove(data.RenderObject);
            }
        }

        public override void Draw(RenderContext context)
        {
            Dispatcher.ForEach(ComponentDatas, entity =>
            {
                var decalComponent = entity.Key;
                var renderDecalData = entity.Value;

                if (decalComponent.Enabled)
                {
                    UpdateRenderObject(decalComponent, renderDecalData);
                }
            });
        }

        private void UpdateRenderObject(DecalComponent decalComponent, RenderDecalData renderDecalData)
        {
            var rendObj = renderDecalData.RenderObject;
            // Transfer all relevant data to the render object, which is the 'final' data to
            // be used by the DecalRootRenderFeature.
            rendObj.Color = decalComponent.Color;
            rendObj.Texture = decalComponent.Texture;
            rendObj.TextureScale = decalComponent.TextureScale;
            rendObj.RenderGroup = decalComponent.RenderGroup;
            rendObj.WorldMatrix = decalComponent.Entity.Transform.WorldMatrix;
        }
    }
}
