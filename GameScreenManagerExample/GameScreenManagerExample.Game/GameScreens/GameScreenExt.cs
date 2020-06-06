using GameScreenManagerExample.GameScreens.SubScreens;
using Stride.Engine;
using System.Diagnostics;
using System.Linq;

namespace GameScreenManagerExample.GameScreens
{
    public static class GameScreenExt
    {
        internal static T GetScreenFromGameScreenScene<T>(this Scene scene) where T : class, IGameScreen
        {
            var gameScreenCtrlEnt = scene.Entities.FirstOrDefault(x => x.Name == GameScreenController.EntityName);
            Debug.Assert(gameScreenCtrlEnt != null, $"Scene {scene.Name} must contain an entity named {GameScreenController.EntityName} at the top level of the scene.");
            var gameScreenCtrl = (GameScreenController)gameScreenCtrlEnt.FirstOrDefault(x => x is GameScreenController gsCtrl && gsCtrl.GameScreen is T);
            var gameScreen = gameScreenCtrl?.GameScreen as T;
            Debug.Assert(gameScreen != null, $"Entity {GameScreenController.EntityName} must contain a {typeof(T).Name} component.");
            return gameScreen;
        }

        internal static T FindScreenFromRootScene<T>(this Scene rootScene) where T : class, IGameScreen
        {
            // This differs from GetScreenFromGameScreenScene where we don't know which scene contains the screen
            foreach (var scene in rootScene.Children)
            {
                var gameScreenCtrlEnt = scene.Entities.FirstOrDefault(x => x.Name == GameScreenController.EntityName);
                var gameScreenCtrl = (GameScreenController)gameScreenCtrlEnt.FirstOrDefault(x => x is GameScreenController gsCtrl && gsCtrl.GameScreen is T);
                if (gameScreenCtrl?.GameScreen is T gameScreen)
                {
                    return gameScreen;
                }
            }
            Debug.Fail("Screen not found.");
            return null;
        }

        internal static T FindSubScreenFromRootScene<T>(this Scene rootScene) where T : class, ISubScreen
        {
            // This differs from GetScreenFromGameScreenScene where we don't know which scene contains the screen
            foreach (var scene in rootScene.Children)
            {
                var subScreenCtrlEnt = scene.Entities.FirstOrDefault(x => x.Name == SubScreenController.EntityName);
                var subScreenCtrl = (SubScreenController)subScreenCtrlEnt?.FirstOrDefault(x => x is SubScreenController ssCtrl && ssCtrl.SubScreen is T);
                if (subScreenCtrl?.SubScreen is T subScreen)
                {
                    return subScreen;
                }
            }
            Debug.Fail("Sub-Screen not found.");
            return null;
        }
    }
}
