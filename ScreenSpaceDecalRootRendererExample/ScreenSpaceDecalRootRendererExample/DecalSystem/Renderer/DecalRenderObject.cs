using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Graphics.GeometricPrimitives;
using Xenko.Rendering;

namespace ScreenSpaceDecalExample.DecalSystem.Renderer
{
    public class DecalRenderObject : RenderObject
    {
        // Shader properties
        public Color4 Color = Color4.White;
        public Texture Texture;
        public float TextureScale = 1;
        public Matrix WorldMatrix = Matrix.Identity;

        public GeometricPrimitive RenderCube;

        public void Prepare(GraphicsDevice graphicsDevice)
        {
            if (RenderCube != null)
            {
                return;
            }

            RenderCube = GeometricPrimitive.Cube.New(graphicsDevice);
            var pipelineState = RenderCube.PipelineState.State;
            pipelineState.BlendState = BlendStates.AlphaBlend;
            pipelineState.DepthStencilState = DepthStencilStates.DepthRead;
        }
    }
}
