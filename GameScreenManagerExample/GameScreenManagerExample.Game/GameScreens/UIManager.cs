using GameScreenManagerExample.GameScreens.PageHandlers;
using GameScreenManagerExample.GameServices;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Engine.Design;
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
    [DefaultEntityComponentProcessor(typeof(UIManagerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    public class UIManager : EntityComponent
    {
        public const string EntityName = "UIManager";

        private ContentManager _content;
        private Scene _uiScene;

        private GameManager _gameManager;
        private SceneManager _sceneManager;

        private Stack<ScreenData> _activeScreens = new Stack<ScreenData>(4);

        [Display(10, "Splash Screen UI")]
        public UrlReference<Prefab> SplashScreenUIUrl;

        [Display(50, "Title Screen UI")]
        public UrlReference<Prefab> TitleScreenUIUrl;

        [Display(51, "New Game Screen UI")]
        public UrlReference<Prefab> NewGameScreenUIUrl;

        [Display(52, "Load Game Screen UI")]
        public UrlReference<Prefab> LoadGameScreenUIUrl;

        [Display(53, "Options Screen UI")]
        public UrlReference<Prefab> OptionsScreenUIUrl;

        [Display(100, "In-Game Screen UI")]
        public UrlReference<Prefab> InGameScreenUIUrl;

        [Display(110, "In-Game Options Screen UI")]
        public UrlReference<Prefab> InGameOptionsScreenUIUrl;

        [DataMemberIgnore]
        public IServiceRegistry Services { get; private set; }

        [DataMemberIgnore]
        public IPageHandler TopPageHandler => _activeScreens.Count > 0 ? _activeScreens.Peek().PageController.PageHandler : null;

        internal void Initialize(GameManager gameManager, SceneManager sceneManager)
        {
            _gameManager = gameManager;
            _sceneManager = sceneManager;
            Services = gameManager.Services;
            _content = Services.GetSafeServiceAs<ContentManager>();
            _uiScene = Entity.Scene;
        }

        public void Start()
        {
        }

        public void Update()
        {
        }

        /// <summary>
        /// Loads the next screen (prefab) immediately.
        /// </summary>
        /// <param name="uiEntityPrefabUrl"></param>
        internal Entity LoadUIEntitySync(UrlReference<Prefab> uiEntityPrefabUrl)
        {
            var prefab = _content.Load(uiEntityPrefabUrl);
            var entity = OnUIPrefabLoaded(prefab);
            return entity;
        }

        /// <summary>
        /// Loads the next screen (prefab) in the background.
        /// </summary>
        /// <param name="uiEntityPrefabUrl"></param>
        internal async Task<Entity> LoadUIEntityAsync(UrlReference<Prefab> uiEntityPrefabUrl)
        {
            var loadPrefabTask = _content.LoadAsync(uiEntityPrefabUrl);
            var prefab = await loadPrefabTask;
            var entity = OnUIPrefabLoaded(prefab);
            return entity;
        }

        private static Entity OnUIPrefabLoaded(Prefab prefab)
        {
            var uiEntities = prefab.Instantiate();
            Debug.Assert(uiEntities.Count == 1, "UI Prefab should only contain one top-level entity.");
            return uiEntities.First();
        }

        /// <summary>
        /// Removes all pushed screens and adds this as the new screen.
        /// </summary>
        /// <param name="uiPageEntity"></param>
        internal void SetAsMainScreen(Entity uiPageEntity)
        {
            PopAllScreens();
            PushScreen(uiPageEntity);
        }

        internal void PushScreen(Entity uiPageEntity)
        {
            var pageCtrl = uiPageEntity.FirstOrDefault(x => x is PageController) as PageController;
            Debug.Assert(pageCtrl != null, $"Entity {PageController.EntityName} must contain a {nameof(PageController)} component.");
            if (!pageCtrl.IsInitialized)
            {
                pageCtrl.Initialize(this, _gameManager, _sceneManager);
            }

            // Add to scene
            _uiScene.Entities.Add(uiPageEntity);

            if (_activeScreens.Count > 0)
            {
                _activeScreens.Peek().PageController.PageHandler.IsTopMostScreen = false;
            }
            _activeScreens.Push(new ScreenData(uiPageEntity, pageCtrl));
            pageCtrl.PageHandler.IsTopMostScreen = true;
            pageCtrl.OnActivate();
        }

        internal IPageHandler PopTopScreen()
        {
            var subScreenData = _activeScreens.Pop();
            subScreenData.PageController.OnDeactivate();
            // Remove from scene
            _uiScene.Entities.Remove(subScreenData.UIEntity);

            if (_activeScreens.Count > 0)
            {
                _activeScreens.Peek().PageController.PageHandler.IsTopMostScreen = true;
            }
            return subScreenData.PageController.PageHandler;
        }

        internal void PopAllScreens()
        {
            while (_activeScreens.Count > 0)
            {
                var subScreenData = _activeScreens.Pop();
                subScreenData.PageController.OnDeactivate();
                bool wasRemoved = _uiScene.Entities.Remove(subScreenData.UIEntity);
                Debug.Assert(wasRemoved);
            }
        }

        private readonly struct ScreenData
        {
            public readonly Entity UIEntity;
            public readonly PageController PageController;

            public ScreenData(Entity uiEntity, PageController pageController)
            {
                UIEntity = uiEntity;
                PageController = pageController;
            }
        }
    }
}
