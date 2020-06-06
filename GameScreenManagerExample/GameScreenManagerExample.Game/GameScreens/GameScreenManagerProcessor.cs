using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;
using System.Collections.Generic;

namespace GameScreenManagerExample.GameScreens
{
    class GameScreenManagerProcessor : EntityProcessor<GameScreenManager>
    {
        private readonly List<GameScreenManager> _pendingStart = new List<GameScreenManager>(1);        // Technically there should only be one manager, but just in case we support more we'll use a list
        private readonly List<GameScreenManager> _processManager = new List<GameScreenManager>(1);

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] GameScreenManager component, [NotNull] GameScreenManager data)
        {
            component.Initialize(Services);
            _pendingStart.Add(component);
        }

        public override void Update(GameTime gameTime)
        {
            foreach (var mgr in _pendingStart)
            {
                mgr.Start();
            }
            foreach (var mgr in _processManager)
            {
                mgr.Update();
            }
            if (_pendingStart.Count > 0)
            {
                _processManager.AddRange(_pendingStart);
            }
            _pendingStart.Clear();
        }
    }
}
