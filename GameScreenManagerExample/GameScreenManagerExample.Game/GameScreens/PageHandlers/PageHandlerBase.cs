using GameScreenManagerExample.GameServices;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using System.Diagnostics;
using System.Linq;

namespace GameScreenManagerExample.GameScreens.PageHandlers
{
    [DataContract(Inherited = true)]
    public abstract class PageHandlerBase : IPageHandler
    {
        protected bool EnableOrDisableUIComponentWhenTopMostChanges = true;
        protected Game Game { get; private set; }
        protected GameManager GameManager { get; private set; }
        protected UIManager UIManager { get; private set; }
        protected SceneManager SceneManager { get; private set; }
        protected SceneSystem SceneSystem { get; private set; }

        private UIComponent _uiComponent;
        /// <summary>
        /// Gets the UIComponent attached to this screen's entity.
        /// This must be set in Game Studio.
        /// </summary>
        [DataMemberIgnore]
        protected UIComponent UIComponent
        {
            get
            {
                _uiComponent ??= OwnerEntity.Get<UIComponent>();
                return _uiComponent;
            }
        }

        private Entity _gameMgrEntity;
        protected Entity GameManagerEntity
        {
            get
            {
                if (_gameMgrEntity == null)
                {
                    var entityManager = SceneSystem.SceneInstance.RootScene.Entities;
                    _gameMgrEntity = entityManager.First(x => x.Name == GameManager.EntityName);      // This entity must exist in the root scene!
                }
                return _gameMgrEntity;
            }
        }

        [DataMemberIgnore]
        public Entity OwnerEntity { get; private set; }

        [DataMemberIgnore]
        public bool IsInitialized { get; private set; }

        private bool _isTopMostScreen = false;
        [DataMemberIgnore]
        public bool IsTopMostScreen
        {
            get => _isTopMostScreen;
            set
            {
                if (_isTopMostScreen != value)
                {
                    _isTopMostScreen = value;
                    OnIsTopMostScreenChanged(value);
                }
                if (EnableOrDisableUIComponentWhenTopMostChanges)
                {
                    UIComponent.Enabled = value;
                }
            }
        }

        public void Initialize(Entity ownerEntity, UIManager uiManager, GameManager gameManager, SceneManager sceneManager)
        {
            Debug.Assert(!IsInitialized, "GameScreen has already been initialized.");
            Game = (Game)uiManager.Services.GetSafeServiceAs<Stride.Games.IGame>();
            GameManager = gameManager;
            UIManager = uiManager;
            SceneManager = sceneManager;
            SceneSystem = uiManager.Services.GetSafeServiceAs<SceneSystem>();
            OwnerEntity = ownerEntity;
            OnInitialize();
            IsInitialized = true;
        }

        public void Deinitialize()
        {
            OnDeinitialize();
        }

        /// <summary>
        /// Called before OnActivate. This should be used for setting up things once.
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// Called after OnDeactivate, when the script is being removed from the scene.
        /// </summary>
        protected virtual void OnDeinitialize() { }

        public virtual void OnActivate() { }

        public virtual void OnDeactivate() { }

        public virtual void Update() { }

        protected virtual void OnIsTopMostScreenChanged(bool newIsTopMostScreen) { }

        /// <summary>
        /// Called after the window's size has been changed.
        /// </summary>
        internal protected virtual void OnScreenSizeChanged(Vector2 newScreenSize) { }
    }
}
