using MultiplayerExample.GameServices;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MultiplayerExample.GameScreens
{
    class UIManagerProcessor : EntityProcessor<UIManager>
    {
        private readonly List<UIManager> _pendingStart = new List<UIManager>(1);        // Technically there should only be one manager, but just in case we support more we'll use a list
        private readonly List<UIManager> _activeManagers = new List<UIManager>(1);
        private readonly List<UIManager> _updatingManagers = new List<UIManager>(1);

        private SceneSystem _sceneSystem;

        protected override void OnSystemAdd()
        {
            _sceneSystem = Services.GetSafeServiceAs<SceneSystem>();
        }

        protected override void OnSystemRemove()
        {
            _sceneSystem = null;
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] UIManager component, [NotNull] UIManager data)
        {
            var entityManager = _sceneSystem.SceneInstance.RootScene.Entities;
            var gameMgrEntity = entityManager.First(x => x.Name == GameManager.EntityName);      // This entity must exist in the root scene!
            var gameManager = gameMgrEntity.Get<GameManager>();
            var sceneManager = gameMgrEntity.Get<SceneManager>();
            Debug.Assert(gameManager != null, $"{nameof(GameManager)} component is missing from entity '{GameManager.EntityName}'.");
            Debug.Assert(sceneManager != null, $"{nameof(SceneManager)} component is missing from entity '{GameManager.EntityName}'.");

            component.Initialize(gameManager, sceneManager);
            _pendingStart.Add(component);
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] UIManager component, [NotNull] UIManager data)
        {
            _pendingStart.Remove(data);
            _activeManagers.Remove(data);
        }

        public override void Update(GameTime gameTime)
        {
            _updatingManagers.AddRange(_pendingStart);      // Done this way to prevent the list being modified while running the update
            foreach (var mgr in _pendingStart)
            {
                mgr.Start();
            }
            _updatingManagers.Clear();
            _updatingManagers.AddRange(_activeManagers);    // Done this way to prevent the list being modified while running the update
            foreach (var mgr in _updatingManagers)
            {
                mgr.Update();
            }
            _updatingManagers.Clear();
            if (_pendingStart.Count > 0)
            {
                _activeManagers.AddRange(_pendingStart);
            }
            _pendingStart.Clear();
        }
    }
}
