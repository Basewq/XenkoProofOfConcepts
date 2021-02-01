using Stride.Core.Mathematics;
using Stride.Core.Storage;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace ObjectInfoRenderTargetExample.ObjectInfoRenderer
{
    public class ObjectInfoSceneRenderer : SceneRendererBase
    {
        private Texture _objectInfoTexture;

        public RenderStage ObjectInfoRenderStage { get; set; }


        protected override void InitializeCore()
        {
            base.InitializeCore();
        }

        protected override void CollectCore(RenderContext context)
        {
            base.CollectCore(context);

            // Fill RenderStage formats
            // This declares the ObjectInfo texture to be (uint, uint) format.
            // Changing this means changing ObjectInfoData, ObjectInfoInputShader, and OioShaderBase.
            ObjectInfoRenderStage.Output = new RenderOutputDescription(renderTargetFormat: PixelFormat.R32G32_UInt, depthStencilFormat: PixelFormat.D32_Float);

            context.RenderView.RenderStages.Add(ObjectInfoRenderStage);
        }

        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
        {
            var commandList = drawContext.CommandList;
            var viewSize = context.RenderView.ViewSize;
            var viewWidth = (int)viewSize.X;
            var viewHeight = (int)viewSize.Y;
            if (_objectInfoTexture == null)
            {
                // TODO: Release resources?
                // TODO: Check if view has resized and create new texture if it has?
                _objectInfoTexture = Texture.New2D(drawContext.GraphicsDevice, width: viewWidth, height: viewHeight,
                    format: ObjectInfoRenderStage.Output.RenderTargetFormat0,
                    textureFlags: TextureFlags.ShaderResource | TextureFlags.RenderTarget,
                    arraySize: 1, usage: GraphicsResourceUsage.Default);
            }

            commandList.ResourceBarrierTransition(_objectInfoTexture, GraphicsResourceState.RenderTarget);
            // Render the picking stage using the current view
            using (drawContext.PushRenderTargetsAndRestore())
            {
                commandList.Clear(_objectInfoTexture, Color.Transparent);
                var depthBufferTexture = commandList.DepthStencilBuffer;
                commandList.ResourceBarrierTransition(depthBufferTexture, GraphicsResourceState.DepthWrite);
                commandList.Clear(depthBufferTexture, DepthStencilClearOptions.DepthBuffer);

                commandList.SetRenderTargetAndViewport(depthBufferTexture, _objectInfoTexture);

                context.RenderSystem.Draw(drawContext, context.RenderView, ObjectInfoRenderStage);
            }

            // Prepare as a shader resource view to be accessible in other render stages
            commandList.ResourceBarrierTransition(_objectInfoTexture, GraphicsResourceState.PixelShaderResource);

            var renderView = drawContext.RenderContext.RenderView;
            foreach (var renderFeature in drawContext.RenderContext.RenderSystem.RenderFeatures)
            {
                if (!(renderFeature is RootEffectRenderFeature rootEffectRenderFeature))
                {
                    continue;
                }

                // This texture is accessible as a resource in shaders in future rendering stages, eg. see
                // ObjectInfoInputShader declaring the following:
                // rgroup PerView.ObjectInfo { stage Texture2D<uint2> ObjectInfoData; }
                // Inherit the shader in ObjectInfoInputShader.xksl to use in your own shader.
                var objectInfoLogicalKey = rootEffectRenderFeature.CreateViewLogicalGroup("ObjectInfo");
                var viewFeature = renderView.Features[renderFeature.Index];

                foreach (var viewLayout in viewFeature.Layouts)
                {
                    var resourceGroup = viewLayout.Entries[renderView.Index].Resources;

                    var objectInfoLogicalGroup = viewLayout.GetLogicalGroup(objectInfoLogicalKey);
                    if (objectInfoLogicalGroup.Hash == ObjectId.Empty)
                    {
                        continue;
                    }

                    resourceGroup.DescriptorSet.SetShaderResourceView(objectInfoLogicalGroup.DescriptorSlotStart, _objectInfoTexture);
                }
            }
        }
    }
}
