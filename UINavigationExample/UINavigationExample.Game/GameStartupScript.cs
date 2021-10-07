using Stride.Engine;
using Stride.Rendering.UI;
using System.Diagnostics;
using System.Linq;
using UINavigationExample.UI;

namespace UINavigationExample
{
    public class GameStartupScript : SyncScript
    {
        public override void Start()
        {
            if (Entity.EntityManager.GetProcessor<UINavigationProcessor>() == null)
            {
                var uiNavProcessor = new UINavigationProcessor();
                Entity.EntityManager.Processors.Add(uiNavProcessor);
                Services.AddService<IUINavigationManager>(uiNavProcessor);
            }
            else
            {
                Debug.Fail("UINavigationProcessor has already been added.");
            }
        }

        public override void Update()
        {
            // Note we can't use Start because the RenderFeature isn't initialized in time.
            var uiRenderFeature = SceneSystem.GraphicsCompositor.RenderFeatures.FirstOrDefault(x => x is UIRenderFeature) as UIRenderFeature;
            Debug.Assert(uiRenderFeature != null, "GraphicsCompositor is missing UIRenderFeature");
            if (!uiRenderFeature.Initialized)
            {
                return;
            }
            var rendererFactory = new GameUIRendererFactory(Services);
            rendererFactory.RegisterToUIRenderFeature(uiRenderFeature);

            Entity.Remove(this);    // Startup has finished, we can remove this script to prevent further action
        }
    }
}
