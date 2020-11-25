using MultiplayerExample.GameServices.SceneHandlers;
using MultiplayerExample.Network;
using MultiplayerExample.Network.SnapshotStores;
using Stride.Engine;
using System.Diagnostics;

namespace MultiplayerExample.GameServices
{
    class LazyLoadedSceneData
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
            return _gameplayScene;
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
    }
}
