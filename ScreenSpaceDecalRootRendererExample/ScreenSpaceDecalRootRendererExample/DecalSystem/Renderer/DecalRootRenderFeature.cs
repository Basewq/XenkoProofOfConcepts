using System;
using Xenko.Core.Mathematics;
using Xenko.Rendering;
using Xenko.Streaming;

namespace ScreenSpaceDecalExample.DecalSystem.Renderer
{
    public class DecalRootRenderFeature : RootRenderFeature //RootEffectRenderFeature
    {
        private DynamicEffectInstance _decalShader;

        public override Type SupportedRenderObjectType => typeof(DecalRenderObject);

        public DecalRootRenderFeature()
        {
            // TODO: Determine the render priority. Lower value means render first.
            // Does this even matter?
            SortKey = 0;
        }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            // Initalize the shader
            _decalShader = new DynamicEffectInstance("DecalShader");
            _decalShader.Initialize(Context.Services);
        }

        public override void Prepare(RenderDrawContext context)
        {
            base.Prepare(context);

            // Register resources usage
            foreach (var renderObject in RenderObjects)
            {
                var decalRendObj = (DecalRenderObject)renderObject;
                Context.StreamingManager?.StreamResources(decalRendObj.Texture, StreamingOptions.LoadAtOnce);
                decalRendObj.Prepare(context.GraphicsDevice);
            }
        }

        public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
        {
            // First do everything that doesn't change per individual render object
            var graphicsDevice = context.GraphicsDevice;
            var graphicsContext = context.GraphicsContext;
            var commandList = context.GraphicsContext.CommandList;

            // Refresh shader, might have changed during runtime
            _decalShader.UpdateEffect(graphicsDevice);

            // Set common shader parameters if needed
            _decalShader.Parameters.Set(TransformationKeys.ViewProjection, renderView.ViewProjection);
            _decalShader.Parameters.Set(TransformationKeys.ViewInverse, Matrix.Invert(renderView.View));

            // Important to release it at the end of the draw, otherwise you'll run out of memory!
            //var depthStencil = context.Resolver.ResolveDepthStencil(commandList.DepthStencilBuffer);
            var depthStencil = context.Resolver.ResolveDepthStencil(graphicsDevice.Presenter.DepthStencilBuffer);       // Must use the Presenter's depth buffer, otherwise it won't appear in the Game Studio
            _decalShader.Parameters.Set(DepthBaseKeys.DepthStencil, depthStencil);

            _decalShader.Parameters.Set(CameraKeys.ViewSize, renderView.ViewSize);
            _decalShader.Parameters.Set(CameraKeys.ZProjection, CameraKeys.ZProjectionACalculate(renderView.NearClipPlane, renderView.FarClipPlane));
            _decalShader.Parameters.Set(CameraKeys.NearClipPlane, renderView.NearClipPlane);
            _decalShader.Parameters.Set(CameraKeys.FarClipPlane, renderView.FarClipPlane);

            for (int index = startIndex; index < endIndex; index++)
            {
                var renderNodeReference = renderViewStage.SortedRenderNodes[index].RenderNode;
                var renderNode = GetRenderNode(renderNodeReference);
                var decalRendObj = (DecalRenderObject)renderNode.RenderObject;

                if (decalRendObj.RenderCube == null)
                {
                    continue;   // Next render object
                }

                // Assign shader parameters
                _decalShader.Parameters.Set(TransformationKeys.WorldInverse, Matrix.Invert(decalRendObj.WorldMatrix));
                _decalShader.Parameters.Set(TransformationKeys.WorldViewProjection, decalRendObj.WorldMatrix * renderView.ViewProjection);
                _decalShader.Parameters.Set(TransformationKeys.WorldView, decalRendObj.WorldMatrix * renderView.View);
                _decalShader.Parameters.Set(DecalShaderKeys.DecalTexture, decalRendObj.Texture);
                _decalShader.Parameters.Set(DecalShaderKeys.TextureScale, decalRendObj.TextureScale);
                _decalShader.Parameters.Set(DecalShaderKeys.DecalColor, decalRendObj.Color);

                decalRendObj.RenderCube.Draw(graphicsContext, _decalShader);
            }

            context.Resolver.ReleaseDepthStenctilAsShaderResource(depthStencil);
        }
    }
}
