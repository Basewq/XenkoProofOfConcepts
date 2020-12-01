using System.Linq;
using System.Threading.Tasks;
using Stride.Input;
using Stride.Engine;
using Stride.Physics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace MultiplayerExample
{
    public class DebugPhysicsShapes : AsyncScript
    {
        public RenderGroup RenderGroup = RenderGroup.Group7;

        public override async Task Execute()
        {
        // Setup rendering in the debug entry point if we have it
        var compositor = SceneSystem.GraphicsCompositor;
        var debugRenderer =
            (((compositor.Game as SceneRendererCollection)?.Children
                .FirstOrDefault() as SceneCameraRenderer)?.Child as SceneRendererCollection)
                ?.FirstOrDefault(x => x is DebugRenderer) as DebugRenderer;
        if (debugRenderer == null)
            return;

        var shapesRenderState = new RenderStage("PhysicsDebugShapes", "Main");
            compositor.RenderStages.Add(shapesRenderState);
            var meshRenderFeature = compositor.RenderFeatures.OfType<MeshRenderFeature>().First();
            meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
            {
                EffectName = "StrideForwardShadingEffect",
                RenderGroup = (RenderGroupMask)(1 << (int)RenderGroup),
                RenderStage = shapesRenderState,
            });
            meshRenderFeature.PipelineProcessors.Add(new WireframePipelineProcessor { RenderStage = shapesRenderState });
            debugRenderer.DebugRenderStages.Add(shapesRenderState);

            var simulation = this.GetSimulation();
            if (simulation != null)
                simulation.ColliderShapesRenderGroup = RenderGroup;

            var enabled = false;
            while (Game.IsRunning)
            {
                if (Input.IsKeyDown(Keys.LeftShift) && Input.IsKeyDown(Keys.LeftCtrl) && Input.IsKeyReleased(Keys.P))
                {
                    if (simulation != null)
                    {
                        if (enabled)
                        {
                            simulation.ColliderShapesRendering = false;
                            enabled = false;
                        }
                        else
                        {
                            simulation.ColliderShapesRendering = true;
                            enabled = true;
                        }
                    }
                }

                await Script.NextFrame();
            }
        }
    }
}
