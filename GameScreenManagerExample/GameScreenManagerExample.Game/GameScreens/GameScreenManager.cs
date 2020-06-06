using GameScreenManagerExample.GameScreens.SubScreens;
using GameScreenManagerExample.Utilities;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Engine.Design;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GameScreenManagerExample.GameScreens
{
    /// <summary>
    /// The main controller of the game screens. This manager's entity is located at the root
    /// of the scene tree and always exists while the game is running.
    /// </summary>
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(GameScreenManagerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    public class GameScreenManager : EntityComponent
    {
        public const string EntityName = "GameScreenManager";

        private readonly Queue<LoadSceneJob> _loadSceneJobs = new Queue<LoadSceneJob>(4);
        private ContentManager _content;
        private SceneSystem _sceneSystem;

        private Scene _activeGameScreenScene;
        private GameScreenController _activeGameScreenController;

        private Stack<SubScreenData> _activeSubScreens = new Stack<SubScreenData>(4);

        [Display(00, "Initial Screen")]
        public UrlReference<Scene> InitialScreenSceneUrl;

        [Display(10, "Splash Screen")]
        public UrlReference<Scene> SplashScreenSceneUrl;

        [Display(50, "Title Screen")]
        public UrlReference<Scene> TitleScreenSceneUrl;

        [Display(100, "In-Game Screen")]
        public UrlReference<Scene> InGameScreenSceneUrl;

        [DataMemberIgnore]
        public IServiceRegistry Services { get; private set; }

        [DataMemberIgnore]
        public IGameScreen ActiveScreen => _activeGameScreenController?.GameScreen;

        [DataMemberIgnore]
        public ISubScreen ActiveSubScreen => _activeSubScreens.Count > 0 ? _activeSubScreens.Peek().SubScreenController.SubScreen : null;

        internal void Initialize(IServiceRegistry services)
        {
            Services = services;
            _content = services.GetSafeServiceAs<ContentManager>();
            _sceneSystem = services.GetSafeServiceAs<SceneSystem>();
        }

        public void Start()
        {
            var initialScene = _content.Load(InitialScreenSceneUrl);  // Not async, we want to load this immediately
            ActivateGameScreenScene(initialScene, unloadCurrentScene: true);
        }

        public void Update()
        {
            if (_loadSceneJobs.Count > 0)
            {
                var job = _loadSceneJobs.Peek();
                if (job.LoadingTask.IsFaulted)
                {
                    // TODO how to handle error?
                    Debug.Fail("Load failed.");
                }
                else if (job.LoadingTask.IsCompleted)
                {
                    _loadSceneJobs.Dequeue();
                    var scene = job.LoadingTask.Result;
                    scene.Name = job.SceneName;
                    job.OnLoadCompleted?.Invoke(scene);
                }
            }
        }

        /// <summary>
        /// Loads the next screen (scene) in the background.
        /// </summary>
        /// <param name="gameScreenUrl"></param>
        /// <param name="onLoadCompleted">The action to take once loading has completed.</param>
        internal void LoadNextGameScreen(UrlReference<Scene> gameScreenUrl, Action<Scene> onLoadCompleted)
        {
            var sceneName = gameScreenUrl.GetContentName();     // Scene names aren't saved in the asset file, so need to get it from the url reference.
            var loadSceneTask = _content.LoadAsync(gameScreenUrl);
            _loadSceneJobs.Enqueue(new LoadSceneJob(sceneName, loadSceneTask, onLoadCompleted));
        }

        internal void SetAsActiveGameScreen(Scene nextGameScreenScene, bool alsoUnloadCurrentScene = true)
        {
            ActivateGameScreenScene(nextGameScreenScene, alsoUnloadCurrentScene);
        }

        private void ActivateGameScreenScene(Scene gameScreenScene, bool unloadCurrentScene)
        {
            if (_activeGameScreenScene != null)
            {
                PopAllSubScreens();

                _activeGameScreenController.OnDeactivate();
                bool wasRemoved = _sceneSystem.SceneInstance.RootScene.Children.Remove(_activeGameScreenScene);
                Debug.Assert(wasRemoved);
                if (unloadCurrentScene)
                {
                    _content.Unload(_activeGameScreenScene);
                }
                _activeGameScreenScene = null;
                _activeGameScreenController = null;
            }
            var gameScreenCtrlEnt = gameScreenScene.Entities.FirstOrDefault(x => x.Name == GameScreenController.EntityName);
            Debug.Assert(gameScreenCtrlEnt != null, $"Scene {gameScreenScene.Name} must contain an entity named {GameScreenController.EntityName} at the top level of the scene.");
            var gameScreenCtrl = gameScreenCtrlEnt.FirstOrDefault(x => x is GameScreenController) as GameScreenController;
            Debug.Assert(gameScreenCtrl != null, $"Entity {GameScreenController.EntityName} must contain a {nameof(GameScreenController)} component.");
            if (!gameScreenCtrl.IsInitialized)
            {
                gameScreenCtrl.Initialize(this);
            }

            // Add the sub-scene
            _sceneSystem.SceneInstance.RootScene.Children.Add(gameScreenScene);

            gameScreenCtrl.OnActivate();

            _activeGameScreenScene = gameScreenScene;
            _activeGameScreenController = gameScreenCtrl;
        }

        internal void PushSubScreen(Scene subScreenScene)
        {
            var subScreenCtrlEnt = subScreenScene.Entities.FirstOrDefault(x => x.Name == SubScreenController.EntityName);
            Debug.Assert(subScreenCtrlEnt != null, $"Scene {subScreenScene.Name} must contain an entity named {SubScreenController.EntityName} at the top level of the scene.");
            var subScreenCtrl = subScreenCtrlEnt.FirstOrDefault(x => x is SubScreenController) as SubScreenController;
            Debug.Assert(subScreenCtrl != null, $"Entity {SubScreenController.EntityName} must contain a {nameof(SubScreenController)} component.");
            if (!subScreenCtrl.IsInitialized)
            {
                subScreenCtrl.Initialize(this);
            }

            // Add the sub-scene
            _sceneSystem.SceneInstance.RootScene.Children.Add(subScreenScene);

            if (_activeSubScreens.Count > 0)
            {
                _activeSubScreens.Peek().SubScreenController.SubScreen.IsTopMostScreen = false;
            }
            subScreenCtrl.SubScreen.IsTopMostScreen = true;
            subScreenCtrl.OnActivate();
            _activeSubScreens.Push(new SubScreenData(subScreenScene, subScreenCtrl));
        }

        internal ISubScreen PopSubScreen(bool unloadScene = true)
        {
            var subScreenData = _activeSubScreens.Pop();
            subScreenData.SubScreenController.OnDeactivate();
            // Remove the sub-scene
            var rootScene = _sceneSystem.SceneInstance.RootScene;
            rootScene.Children.Remove(subScreenData.SubScreenScene);
            if (unloadScene)
            {
                _content.Unload(subScreenData.SubScreenScene);
            }

            if (_activeSubScreens.Count > 0)
            {
                _activeSubScreens.Peek().SubScreenController.SubScreen.IsTopMostScreen = true;
            }
            return subScreenData.SubScreenController.SubScreen;
        }

        internal void PopAllSubScreens(bool unloadScenes = true)
        {
            var rootScene = _sceneSystem.SceneInstance.RootScene;
            while (_activeSubScreens.Count > 0)
            {
                var subScreenData = _activeSubScreens.Pop();
                subScreenData.SubScreenController.OnDeactivate();
                bool wasRemoved = rootScene.Children.Remove(subScreenData.SubScreenScene);
                Debug.Assert(wasRemoved);
                if (unloadScenes)
                {
                    _content.Unload(subScreenData.SubScreenScene);
                }
            }
        }

        private readonly struct LoadSceneJob
        {
            public readonly string SceneName;
            public readonly Task<Scene> LoadingTask;
            public readonly Action<Scene> OnLoadCompleted;

            public LoadSceneJob(string sceneName, Task<Scene> loadingTask, Action<Scene> onLoadCompleted)
            {
                SceneName = sceneName;
                LoadingTask = loadingTask;
                OnLoadCompleted = onLoadCompleted;
            }
        }

        private readonly struct SubScreenData
        {
            public readonly Scene SubScreenScene;
            public readonly SubScreenController SubScreenController;

            public SubScreenData(Scene subScreenScene, SubScreenController subScreenController)
            {
                SubScreenScene = subScreenScene;
                SubScreenController = subScreenController;
            }
        }
    }
}
