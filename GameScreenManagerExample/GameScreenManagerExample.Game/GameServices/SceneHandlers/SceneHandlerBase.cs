using GameScreenManagerExample.GameScreens;
using Stride.Core;
using Stride.Engine;
using System.Diagnostics;

namespace GameScreenManagerExample.GameServices.SceneHandlers
{
    [DataContract(Inherited = true)]
    public abstract class SceneHandlerBase : ISceneHandler
    {
        protected GameManager GameManager { get; private set; }
        protected SceneManager SceneManager { get; private set; }

        protected UIManager UIManager { get; private set; }

        protected SceneSystem SceneSystem { get; private set; }

        [DataMemberIgnore]
        public Entity OwnerEntity { get; private set; }

        [DataMemberIgnore]
        public bool IsInitialized { get; private set; }

        public void Initialize(Entity ownerEntity, SceneManager sceneManager, GameManager gameManager, UIManager uiManager)
        {
            Debug.Assert(!IsInitialized, "SceneController has already been initialized.");
            GameManager = gameManager;
            SceneManager = sceneManager;
            UIManager = uiManager;
            SceneSystem = gameManager.Services.GetSafeServiceAs<SceneSystem>();
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
    }
}
