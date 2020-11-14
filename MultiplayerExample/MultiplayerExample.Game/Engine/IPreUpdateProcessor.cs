using Stride.Games;

namespace MultiplayerExample.Engine
{
    interface IPreUpdateProcessor : IInGameProcessor
    {
        /// <summary>
        /// Only called if <see cref="IInGameProcessor.IsEnabled"/> is true.
        /// Called before the physics step and before <see cref="Stride.Engine.EntityProcessor.Update(GameTime)"/>.
        /// </summary>
        void PreUpdate(GameTime gameTime);
    }
}
