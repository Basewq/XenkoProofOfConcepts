using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;
using System.Collections.Generic;
using System.Diagnostics;

namespace GameScreenManagerExample.GameServices
{
    class SceneManagerProcessor : EntityProcessor<SceneManager>
    {
        private readonly List<SceneManager> _pendingStart = new List<SceneManager>(1);        // Technically there should only be one manager, but just in case we support more we'll use a list
        private readonly List<SceneManager> _activeManagers = new List<SceneManager>(1);

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] SceneManager component, [NotNull] SceneManager data)
        {
            var gameManager = entity.Get<GameManager>();
            Debug.Assert(entity.Name == GameManager.EntityName, $"{nameof(GameManager)} component must be attached to entity {GameManager.EntityName}.");
            Debug.Assert(gameManager != null, $"Entity '{entity.Name}' is missing {nameof(GameManager)} component.");
            component.Initialize(gameManager, Services);
            _pendingStart.Add(component);
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] SceneManager component, [NotNull] SceneManager data)
        {
            _pendingStart.Remove(data);
            _activeManagers.Remove(data);
        }

        public override void Update(GameTime gameTime)
        {
            foreach (var mgr in _pendingStart)
            {
                mgr.Start();
            }
            foreach (var mgr in _activeManagers)
            {
                mgr.Update();
            }
            if (_pendingStart.Count > 0)
            {
                _activeManagers.AddRange(_pendingStart);
            }
            _pendingStart.Clear();
        }
    }
}
