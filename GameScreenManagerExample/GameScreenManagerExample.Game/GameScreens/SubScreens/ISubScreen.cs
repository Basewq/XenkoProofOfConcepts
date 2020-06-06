using Stride.Engine;

namespace GameScreenManagerExample.GameScreens.SubScreens
{
    /// <summary>
    /// A <see cref="IGameScreen"/> is made up of one or more <see cref="ISubScreen"/>s.
    /// Sub-screens are usually used for menus, and are tracked on a stack for navigation.
    /// </summary>
    public interface ISubScreen
    {
        /// <summary>
        /// The entity that owns this sub-screen.
        /// </summary>
        Entity OwnerEntity { get; }

        bool IsTopMostScreen { get; }

        bool IsInitialized { get; }

        /// <summary>
        /// Called once after this screen has been created, but before it has been added to the scene.
        /// </summary>
        /// <param name="gameScreenManager"></param>
        void Initialize(Entity ownerEntity, GameScreenManager gameScreenManager);

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
