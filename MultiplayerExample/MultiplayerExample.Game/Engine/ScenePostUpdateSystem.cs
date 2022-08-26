using Stride.Core;
using Stride.Engine;
using Stride.Games;

namespace MultiplayerExample.Engine
{
    class ScenePostUpdateSystem : GameSystemBase
    {
        private readonly SceneSystem _sceneSystem;

        public ScenePostUpdateSystem(IServiceRegistry registry, SceneSystem sceneSystem) : base(registry)
        {
            Enabled = true;
            _sceneSystem = sceneSystem;
        }

        public override void Update(GameTime gameTime)
        {
            var processors = _sceneSystem.SceneInstance?.Processors;
            if (processors == null)
            {
                return;
            }

            foreach (var p in processors)
            {
                if (p is not IPostUpdateProcessor proc)
                {
                    continue;
                }
                if (proc.IsEnabled)
                {
                    proc.PostUpdate(gameTime);
                }
            }

            foreach (var p in processors)
            {
                if (p is not INetworkPostUpdateProcessor proc)
                {
                    continue;
                }
                if (proc.IsEnabled)
                {
                    proc.PostUpdate(gameTime);
                }
            }
        }
    }
}
