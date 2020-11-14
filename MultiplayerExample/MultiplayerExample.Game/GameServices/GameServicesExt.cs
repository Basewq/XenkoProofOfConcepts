using MultiplayerExample.GameServices.SceneHandlers;
using Stride.Engine;
using System.Diagnostics;
using System.Linq;

namespace MultiplayerExample.GameServices
{
    static class GameServicesExt
    {
        internal static GameManager GetGameManagerFromRootScene(this SceneSystem sceneSystem)
        {
            var rootScene = sceneSystem.SceneInstance.RootScene;
            var entityManager = rootScene.Entities;
            var gameMgrEntity = entityManager.First(x => x.Name == GameManager.EntityName);      // This entity must exist in the root scene!
            var gameManager = gameMgrEntity.Get<GameManager>();
            Debug.Assert(gameManager != null, $"{nameof(GameManager)} component is missing from entity '{GameManager.EntityName}'.");
            return gameManager;
        }

        internal static SceneManager GetSceneManagerFromRootScene(this SceneSystem sceneSystem)
        {
            var rootScene = sceneSystem.SceneInstance.RootScene;
            var entityManager = rootScene.Entities;
            var gameMgrEntity = entityManager.First(x => x.Name == GameManager.EntityName);      // This entity must exist in the root scene!
            var sceneManager = gameMgrEntity.Get<SceneManager>();
            Debug.Assert(sceneManager != null, $"{nameof(SceneManager)} component is missing from entity '{GameManager.EntityName}'.");
            return sceneManager;
        }

        internal static T GetSceneHandlerFromScene<T>(this Scene scene) where T : class, ISceneHandler
        {
            var entityManager = scene.Entities;
            var sceneCtrlEntity = entityManager.First(x => x.Name == SceneController.EntityName);      // This entity must exist in the root scene!
            var sceneCtrl = sceneCtrlEntity.Get<SceneController>();
            Debug.Assert(sceneCtrl != null, $"{nameof(SceneController)} component is missing from entity '{SceneController.EntityName}'.");
            return sceneCtrl.SceneHandler as T;
        }
    }
}
