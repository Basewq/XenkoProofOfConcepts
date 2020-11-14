using Stride.Games;

namespace MultiplayerExample.Engine
{
    // Special case: INetworkPostUpdateProcessor must occur after the normal IPostUpdateProcessor.
    interface INetworkPostUpdateProcessor : IInGameProcessor
    {
        /// <summary>
        /// Only called if <see cref="IInGameProcessor.IsEnabled"/> is true.
        /// </summary>
        void PostUpdate(GameTime gameTime);
    }
}
