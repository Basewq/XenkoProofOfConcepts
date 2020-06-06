using GameScreenManagerExample.GameServices;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using System.Diagnostics;
using System.Linq;

namespace GameScreenManagerExample.GameScreens
{
    [DataContract(Inherited = true)]
    public abstract class GameScreenBase : IGameScreen
    {
        protected Game Game { get; private set; }
        protected GameScreenManager GameScreenManager { get; private set; }
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

        public void Initialize(Entity ownerEntity, GameScreenManager gameScreenManager)
        {
            Debug.Assert(!IsInitialized, "GameScreen has already been initialized.");
            Game = (Game)gameScreenManager.Services.GetSafeServiceAs<Stride.Games.IGame>();
            GameScreenManager = gameScreenManager;
            SceneSystem = gameScreenManager.Services.GetSafeServiceAs<SceneSystem>();
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
        protected virtual void OnInitialize()
        {
        }

        /// <summary>
        /// Called after OnDeactivate, when the script is being removed from the scene.
        /// </summary>
        protected virtual void OnDeinitialize()
        {
        }

        public virtual void OnActivate() { }

        public virtual void OnDeactivate() { }

        public virtual void Update() { }

        /// <summary>
        /// Called after the window's size has been changed.
        /// </summary>
        internal protected virtual void OnScreenSizeChanged(Vector2 screenSize) { }
    }
}
