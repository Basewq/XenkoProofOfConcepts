using MultiplayerExample.Core;
using MultiplayerExample.GameScreens;
using MultiplayerExample.GameServices.SceneHandlers;
using MultiplayerExample.Utilities;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Engine.Design;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MultiplayerExample.GameServices
{
    /// <summary>
    /// The main controller of the game screens. This manager's entity is located at the root
    /// of the scene tree and always exists while the game is running.
    /// </summary>
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(SceneManagerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    public class SceneManager : EntityComponent
    {
        private GameManager _gameManager;
        private IServiceRegistry _services;

        private ContentManager _content;
        private SceneSystem _sceneSystem;

        private Scene _activeMainScene;
        private SceneController _activeMainSceneController;
        private UIManager _uiManager;

        [Display(-100, "UI System")]
        public UrlReference<Prefab> UiSystemUrl;

        [Display(05, "Root Client Only Data")]
        public UrlReference<Scene> RootClientOnlyDataSceneUrl;

        [Display(10, "Initial Screen")]
        public UrlReference<Scene> InitialScreenSceneUrl;

        //[Display(10, "Splash Screen")]
        //public UrlReference<Scene> SplashScreenSceneUrl;

        [Display(50, "Title Screen")]
        public UrlReference<Scene> TitleScreenSceneUrl;

        [Display(100, "In-Game")]
        public UrlReference<Scene> InGameSceneUrl;

        [Display(110, "In-Game Server Only Data")]
        public UrlReference<Scene> InGameServerOnlyDataSceneUrl;

        [DataMemberIgnore]
        public ISceneHandler ActiveMainSceneHandler => _activeMainSceneController?.SceneHandler;

        internal void Initialize(GameManager gameManager, IServiceRegistry services)
        {
            _gameManager = gameManager;
            _services = services;
            _content = services.GetSafeServiceAs<ContentManager>();
            _sceneSystem = services.GetSafeServiceAs<SceneSystem>();
        }

        public void Start()
        {
            if (!_gameManager.GameEngineContext.IsServer)
            {
                LoadUiSystem();
                var initialScene = _content.Load(InitialScreenSceneUrl);    // Not async, we want to load this immediately
                ActivateMainScene(initialScene, unloadCurrentMainScene: true);
            }
            else
            {
                var inGameScene = _content.Load(InGameSceneUrl);            // Not async, we want to load this immediately
                var serverOnlyDataScene = _content.Load(InGameServerOnlyDataSceneUrl);
                serverOnlyDataScene.MergeSceneTo(inGameScene);
                ActivateMainScene(inGameScene, unloadCurrentMainScene: true);
            }
        }

        public void Update()
        {
        }

        private void LoadUiSystem()
        {
            // The reason the UI 'system' is not immediately part of the root scene is because we may want
            // to remove the UI from the game for a headless game, though additional code may be
            // required to guard against calls to UIManager when it has not been set up.

            if (UiSystemUrl?.IsEmpty ?? true)
            {
                throw new InvalidOperationException($"{nameof(UiSystemUrl)} is not set.");
            }
            if (_sceneSystem.SceneInstance.RootScene.Entities.Any(x => x.Name == UIManager.EntityName))
            {
                throw new InvalidOperationException($"UI System has already been loaded.");
            }
            var uiSystemPrefab = _content.Load(UiSystemUrl);
            Debug.Assert(uiSystemPrefab.Entities.Exists(x => x.Name == UIManager.EntityName), $"UI System must contain an entity named {UIManager.EntityName}.");

            var uiSystem = uiSystemPrefab.Instantiate();
            var uiManagerEnt = uiSystem.First(x => x.Name == UIManager.EntityName);
            var uiManager = uiManagerEnt.Get<UIManager>();
            Debug.Assert(uiManager != null, $"Entity {UIManager.EntityName} must contain a {nameof(UIManager)} component.");

            // Add to root scene
            _sceneSystem.SceneInstance.RootScene.Entities.AddRange(uiSystem);

            _uiManager = uiManager;
        }

        /// <summary>
        /// Loads the scene immediately.
        /// </summary>
        /// <param name="sceneUrl"></param>
        internal Scene LoadSceneSync(UrlReference<Scene> sceneUrl)
        {
            var sceneName = sceneUrl.GetContentName();     // Scene names aren't saved in the asset file, so need to get it from the url reference.
            var scene = _content.Load(sceneUrl);
            scene.Name = sceneName;
            return scene;
        }

        /// <summary>
        /// Loads the next screen (scene) in the background.
        /// </summary>
        /// <param name="sceneUrl"></param>
        internal async Task<Scene> LoadSceneAsync(UrlReference<Scene> sceneUrl)
        {
            var sceneName = sceneUrl.GetContentName();     // Scene names aren't saved in the asset file, so need to get it from the url reference.
            var loadSceneTask = _content.LoadAsync(sceneUrl);
            var scene = await loadSceneTask;
            if (loadSceneTask.IsCompleted)
            {
                scene.Name = sceneName;
            }
            return scene;
        }

        internal void SetAsActiveMainScene(Scene nextScene, bool alsoUnloadCurrentMainScene = true)
        {
            ActivateMainScene(nextScene, alsoUnloadCurrentMainScene);
        }

        private void ActivateMainScene(Scene scene, bool unloadCurrentMainScene)
        {
            if (_activeMainScene != null)
            {
                _activeMainSceneController.OnDeactivate();
                bool wasRemoved = _sceneSystem.SceneInstance.RootScene.Children.Remove(_activeMainScene);
                Debug.Assert(wasRemoved);
                if (unloadCurrentMainScene)
                {
                    _content.Unload(_activeMainScene);
                }
                _activeMainScene = null;
                _activeMainSceneController = null;
            }
            var gameScreenCtrlEnt = scene.Entities.FirstOrDefault(x => x.Name == SceneController.EntityName);
            Debug.Assert(gameScreenCtrlEnt != null, $"Scene {scene.Name} must contain an entity named {SceneController.EntityName} at the top level of the scene.");
            var gameScreenCtrl = gameScreenCtrlEnt.FirstOrDefault(x => x is SceneController) as SceneController;
            Debug.Assert(gameScreenCtrl != null, $"Entity {SceneController.EntityName} must contain a {nameof(SceneController)} component.");
            if (!gameScreenCtrl.IsInitialized)
            {
                gameScreenCtrl.Initialize(this, _gameManager, _uiManager);
            }

            // Add the sub-scene
            _sceneSystem.SceneInstance.RootScene.Children.Add(scene);

            gameScreenCtrl.OnActivate();

            _activeMainScene = scene;
            _activeMainSceneController = gameScreenCtrl;
        }
    }
}
