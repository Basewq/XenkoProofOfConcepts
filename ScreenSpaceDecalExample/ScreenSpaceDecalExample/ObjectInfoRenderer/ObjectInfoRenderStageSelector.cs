using System.ComponentModel;
using Stride.Rendering;

namespace ScreenSpaceDecalExample.ObjectInfoRenderer
{
    public class ObjectInfoRenderStageSelector : RenderStageSelector
    {
        [DefaultValue(RenderGroupMask.All)]
        public RenderGroupMask RenderGroup { get; set; } = RenderGroupMask.All;

        [DefaultValue(null)]
        public RenderStage ObjectInfoRenderStage { get; set; }

        public string EffectName { get; set; }

        public override void Process(RenderObject renderObject)
        {
            if (((RenderGroupMask)(1U << (int)renderObject.RenderGroup) & RenderGroup) != 0)
            {
                //var renderMesh = (RenderMesh)renderObject;
                // TODO ignore renderMesh.MaterialPass.HasTransparency?
                var renderStage = ObjectInfoRenderStage;
                if (renderStage != null)
                {
                    renderObject.ActiveRenderStages[renderStage.Index] = new ActiveRenderStage(EffectName);
                }
            }
        }
    }
}
