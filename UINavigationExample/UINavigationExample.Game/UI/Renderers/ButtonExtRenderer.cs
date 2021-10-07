using Stride.Core;
using Stride.Core.Mathematics;
using Stride.UI;
using Stride.UI.Renderers;

namespace UINavigationExample.UI.Renderers
{
    class ButtonExtRenderer : ElementRenderer
    {
        public ButtonExtRenderer(IServiceRegistry services)
            : base(services)
        {
        }

        public override void RenderColor(UIElement element, UIRenderingContext context)
        {
            base.RenderColor(element, context);

            var button = (ButtonExt)element;
            var sprite = button.ButtonImage;
            if (sprite?.Texture == null)
                return;

            var color = element.RenderOpacity * Color.White;

            // Note: the original ButtonRenderer code uses internal fields (to avoid copying structs)
            // We don't expect too many UI, so this shouldn't be a big performance hit
            var elemWorldMatrix = element.WorldMatrix;
            var elemRenderSize = element.RenderSize;
            var spriteRegion = sprite.Region;
            var spriteBorder = sprite.Borders;
            Batch.DrawImage(sprite.Texture, ref elemWorldMatrix, ref spriteRegion, ref elemRenderSize, ref spriteBorder, ref color, context.DepthBias, sprite.Orientation);
        }
    }
}
