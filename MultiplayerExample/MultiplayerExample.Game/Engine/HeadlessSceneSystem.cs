using Stride.Core;
using Stride.Core.Serialization.Contents;
using Stride.Engine;

namespace MultiplayerExample.Engine
{
    class HeadlessSceneSystem : SceneSystem
    {
        public HeadlessSceneSystem(IServiceRegistry registry) : base(registry)
        {
        }

        protected override void LoadContent()
        {
            // TODO: Need to suppress SceneSystem.LoadContent() because it loads graphics related objects
            var content = Services.GetSafeServiceAs<ContentManager>();

            // Preload the scene if it exists
            if (InitialSceneUrl != null && content.Exists(InitialSceneUrl))
            {
                SceneInstance = new SceneInstance(Services, content.Load<Scene>(InitialSceneUrl));
            }
        }
    }
}
