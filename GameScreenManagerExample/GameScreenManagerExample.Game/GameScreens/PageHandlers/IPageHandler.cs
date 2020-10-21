using GameScreenManagerExample.GameServices;
using Stride.Engine;

namespace GameScreenManagerExample.GameScreens.PageHandlers
{
    /// <summary>
    /// UI is made up of one or more <see cref="IPageHandler"/>s, and are tracked on a stack in <see cref="UIManager"/>.
    /// </summary>
    public interface IPageHandler
    {
        /// <summary>
        /// The entity that owns this <see cref="IPageHandler"/>.
        /// </summary>
        Entity OwnerEntity { get; }

        bool IsTopMostScreen { get; }

        bool IsInitialized { get; }

        /// <summary>
        /// Called once after this screen has been created, but before it has been added to the scene.
        /// </summary>
        /// <param name="uiManager"></param>
        void Initialize(Entity ownerEntity, UIManager uiManager, GameManager gameManager, SceneManager sceneManager);

        /// <summary>
        /// Called when this screen is being destroyed.
        /// </summary>
        void Deinitialize();

        /// <summary>
        /// Called immediately after this screen is added to the scene.
        /// </summary>
        void OnActivate();

        /// <summary>
        /// Called just before this screen removed from the scene tree.
        /// </summary>
        void OnDeactivate();
    }
}
