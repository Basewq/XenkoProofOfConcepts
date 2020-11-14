using Stride.Games;

namespace MultiplayerExample.Engine
{
    interface IPostUpdateProcessor : IInGameProcessor
    {
        /// <summary>
        /// Only called if <see cref="IInGameProcessor.IsEnabled"/> is true.
        /// Called after the physics step and after <see cref="Stride.Engine.EntityProcessor.Update(GameTime)"/>.
        /// </summary>
        void PostUpdate(GameTime gameTime);
    }
}
