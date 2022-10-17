using Stride.Rendering;
using Stride.Rendering.UI;

namespace DialogueTextControlExample.UI
{
    /// <summary>
    /// Required to use this instead of the default one, to allow registering our custom controls.
    /// Be sure to set this in the GraphicsCompositor.
    /// </summary>
    public class UIRenderFeatureExt : UIRenderFeature
    {
        private GameUIRendererFactory _rendererFactory;

        protected override void InitializeCore()
        {
            base.InitializeCore();

            _rendererFactory = new GameUIRendererFactory(RenderSystem.Services);
            _rendererFactory.RegisterToUIRenderFeature(this);
        }
    }
}
