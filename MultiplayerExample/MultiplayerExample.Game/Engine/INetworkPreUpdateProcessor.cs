using Stride.Games;

namespace MultiplayerExample.Engine
{
    // Special case: INetworkPreUpdateProcessor must occur before the normal IPreUpdateProcessor.
    interface INetworkPreUpdateProcessor : IInGameProcessor
    {
        /// <summary>
        /// Only called if <see cref="IInGameProcessor.IsEnabled"/> is true.
        /// </summary>
        void PreUpdate(GameTime gameTime);
    }
}
