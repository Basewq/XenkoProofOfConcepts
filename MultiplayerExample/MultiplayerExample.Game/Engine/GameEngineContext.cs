namespace MultiplayerExample.Engine
{
    /// <summary>
    /// The engine context object can be used in entity processors to determine whether it is running in a
    /// client engine or a server engine (retrieved via <see cref="Stride.Core.IServiceRegistry"/>, eg the Services property on an entity processor).
    /// </summary>
    class GameEngineContext
    {
        public readonly bool IsServer;  // TODO: GameMode? DedicatedServer/ListenServer/Client/SinglePlayer
        public bool IsClient => !IsServer;

        public GameEngineContext(bool isServer)
        {
            IsServer = isServer;
        }
    }
}
