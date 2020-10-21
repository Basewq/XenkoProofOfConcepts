using GameScreenManagerExample.GameScreens;
using Stride.Engine;

namespace GameScreenManagerExample.GameServices.SceneHandlers
{
    public interface ISceneHandler
    {
        /// <summary>
        /// The entity that owns this <see cref="ISceneHandler"/>.
        /// </summary>
        Entity OwnerEntity { get; }

        bool IsInitialized { get; }

        /// <summary>
        /// Called once after the scene has been created, but before it has been added to the scene.
        /// </summary>
        void Initialize(Entity ownerEntity, SceneManager sceneManager, GameManager gameManager, UIManager uiManager);

        /// <summary>
        /// Called when this <see cref="ISceneHandler"/> is being destroyed.
        /// </summary>
        void Deinitialize();

        /// <summary>
        /// Called immediately after this <see cref="ISceneHandler"/> is added to the scene.
        /// </summary>
        void OnActivate();

        /// <summary>
        /// Called just before this <see cref="ISceneHandler"/> removed from the scene tree.
        /// </summary>
        void OnDeactivate();
    }
}