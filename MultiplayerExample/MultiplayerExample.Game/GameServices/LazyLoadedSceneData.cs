using MultiplayerExample.GameServices.SceneHandlers;
using MultiplayerExample.Network;
using MultiplayerExample.Network.SnapshotStores;
using Stride.Engine;
using System;
using System.Diagnostics;

namespace MultiplayerExample.GameServices
{
    class LazyLoadedSceneData : IDisposable
    {
        private readonly SceneSystem _sceneSystem;

        private Scene _gameplayScene;
        private MovementSnapshotsInputProcessor _movementSnapshotsInputProcessor;

        private GameManager _gameManager;

        private NetworkAssetDefinitions _assetDefinitions;

        public LazyLoadedSceneData(SceneSystem sceneSystem)
        {
            _sceneSystem = sceneSystem;
        }

        /// <summary>
        /// The scene where we actually add/remove gameplay related entities.
        /// <br />
        /// <b>Warning:</b> Do not hold a direct reference to the returned gameplay scene because it can potentially change.
        /// </summary>
        public Scene GetGameplayScene()
        {
            if (_gameplayScene != null)
            {
                return _gameplayScene;
            }

            // Entities are added to the InGameScreen scene rather than the root scene
            var sceneManager = _sceneSystem.GetSceneManagerFromRootScene();
            Debug.Assert(sceneManager != null, $"SceneManager entity must contain {nameof(SceneManager)} component.");
            Debug.Assert(sceneManager.ActiveMainSceneHandler is InGameSceneHandler, "Must be in-game.");
            _gameplayScene = sceneManager.ActiveMainSceneHandler.Scene;

            sceneManager.MainSceneChanged += OnMainSceneChanged;

            return _gameplayScene;
        }

        private void OnMainSceneChanged(Scene newScene)
        {
            _gameplayScene = null;
        }

        public MovementSnapshotsInputProcessor GetMovementSnapshotsInputProcessor()
        {
            _movementSnapshotsInputProcessor ??= _sceneSystem.SceneInstance.GetProcessor<MovementSnapshotsInputProcessor>();
            Debug.Assert(_movementSnapshotsInputProcessor != null, $"You cannot call this method yet because {nameof(MovementSnapshotsInputProcessor)} hasn't been created yet.");
            return _movementSnapshotsInputProcessor;
        }

        public GameManager GetGameManager()
        {
            _gameManager ??= _sceneSystem.GetGameManagerFromRootScene();
            return _gameManager;
        }

        public NetworkAssetDefinitions GetNetworkAssetDefinitions()
        {
            var gameManager = GetGameManager();
            _assetDefinitions ??= gameManager.Entity.Get<NetworkAssetDefinitions>();
            Debug.Assert(_assetDefinitions != null, $"GameManager entity must contain {nameof(NetworkAssetDefinitions)} component.");
            return _assetDefinitions;
        }

        public void Dispose()
        {
            var sceneManager = _sceneSystem.GetSceneManagerFromRootScene();
            sceneManager.MainSceneChanged -= OnMainSceneChanged;
        }
    }
}
