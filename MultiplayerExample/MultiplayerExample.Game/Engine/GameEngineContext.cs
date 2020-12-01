namespace MultiplayerExample.Engine
{
    /// <summary>
    /// The engine context object can be used in entity processors to determine whether it is running in a
    /// client engine or a server engine (retrieved via <see cref="Stride.Core.IServiceRegistry"/>,
    /// ie. the Services property on an entity processor).
    /// If <see cref="IsClient"/> is true, the game has graphics/audio capability.
    /// </summary>
    class GameEngineContext
    {
        public readonly bool IsClient;
        public bool IsServer => !IsClient;

        public GameEngineContext(bool isClient)
        {
            IsClient = isClient;
        }
    }
}
