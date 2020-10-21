using Stride.Engine;
using System.Diagnostics;
using System.Linq;

namespace GameScreenManagerExample.GameScreens
{
    static class UIManagerExt
    {
        internal static UIManager GetUIManagerFromRootScene(this SceneSystem sceneSystem)
        {
            var rootScene = sceneSystem.SceneInstance.RootScene;
            var entityManager = rootScene.Entities;
            var gameMgrEntity = entityManager.First(x => x.Name == UIManager.EntityName);      // This entity must exist in the root scene!
            var gameManager = gameMgrEntity.Get<UIManager>();
            Debug.Assert(gameManager != null, $"{nameof(UIManager)} component is missing from entity '{UIManager.EntityName}'.");
            return gameManager;
        }
    }
}
