using Stride.Core;
using Stride.Engine;
using Stride.Games;

namespace MultiplayerExample.Engine
{
    class ScenePreUpdateSystem : GameSystemBase
    {
        private readonly SceneSystem _sceneSystem;

        public ScenePreUpdateSystem(IServiceRegistry registry, SceneSystem sceneSystem) : base(registry)
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
                if (p is not INetworkPreUpdateProcessor proc)
                {
                    continue;
                }
                if (proc.IsEnabled)
                {
                    proc.PreUpdate(gameTime);
                }
            }

            foreach (var p in processors)
            {
                if (p is not IPreUpdateProcessor proc)
                {
                    continue;
                }
                if (proc.IsEnabled)
                {
                    proc.PreUpdate(gameTime);
                }
            }
        }
    }
}
