using Stride.Engine;

namespace GameScreenManagerExample.GameScreens
{
    /// <summary>
    /// A game screen represents a discrete part of a game, eg. Splash Screen, Title Screen, In-Game Screen, etc.
    /// Only one game screen can be active at a time, and may have multiple <see cref="Scene"/>s/<see cref="ISubScreen"/>s.
    /// The entity with the <see cref="IGameScreen"/> must be placed at the root of the <see cref="Scene"/>.
    /// </summary>
    public interface IGameScreen
    {
        /// <summary>
        /// The entity that owns this game screen.
        /// </summary>
        Entity OwnerEntity { get; }

        bool IsInitialized { get; }

        /// <summary>
        /// Called once after this screen has been created, but before it has been added to the scene tree.
        /// If the screen has been deactivated but not deinitialized/destroyed, this will not be called again.
        /// </summary>
        void Initialize(Entity ownerEntity, GameScreenManager gameScreenManager);

        /// <summary>
        /// Called when this screen is being destroyed.
        /// </summary>
        void Deinitialize();

        /// <summary>
        /// Called immediately after this screen is added to the scene tree.
        /// </summary>
        void OnActivate();

        /// <summary>
        /// Called just before this screen removed from the scene tree.
        /// </summary>
        void OnDeactivate();
    }
}