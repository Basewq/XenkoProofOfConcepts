using MultiplayerExample.GameScreens.PageHandlers;
using Stride.Engine;
using System.Diagnostics;
using System.Linq;

namespace MultiplayerExample.GameScreens
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

        internal static T GetPageHandlerFromUIPageEntity<T>(this Entity uiPageEntity) where T : class, IPageHandler
        {
            Debug.Assert(uiPageEntity.Name == PageController.EntityName, $"Entity is not named '{PageController.EntityName}'.");
            var pageCtrl = uiPageEntity.Get<PageController>();
            Debug.Assert(pageCtrl != null, $"{nameof(PageController)} component is missing from entity '{PageController.EntityName}'.");
            return pageCtrl.PageHandler as T;
        }
    }
}
