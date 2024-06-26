using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;
using System.Collections.Generic;

namespace MultiplayerExample.GameServices
{
    class SceneControllerProcessor : EntityProcessor<SceneController>
    {
        private readonly List<SceneController> _pendingStart = new List<SceneController>(1);        // Technically there should only be one manager, but just in case we support more we'll use a list
        private readonly List<SceneController> _activeControllers = new List<SceneController>(1);
        private readonly List<SceneController> _updatingControllers = new List<SceneController>(1);

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] SceneController component, [NotNull] SceneController data)
        {
            _pendingStart.Add(component);

            // Note that initialization occurs in SceneManager rather than here.
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] SceneController component, [NotNull] SceneController data)
        {
            _pendingStart.Remove(data);
            _activeControllers.Remove(data);

            component.Deinitialize();
        }

        public override void Update(GameTime gameTime)
        {
            //foreach (var mgr in _pendingStart)
            //{
            //    mgr.Start();
            //}
            _updatingControllers.AddRange(_activeControllers);    // Done this way to prevent the list being modified while running the update
            foreach (var mgr in _updatingControllers)
            {
                mgr.Update();
            }
            _updatingControllers.Clear();
            if (_pendingStart.Count > 0)
            {
                _activeControllers.AddRange(_pendingStart);
            }
            _pendingStart.Clear();
        }
    }
}
