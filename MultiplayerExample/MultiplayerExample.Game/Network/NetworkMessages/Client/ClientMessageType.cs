namespace MultiplayerExample.Network.NetworkMessages.Client
{
    enum ClientMessageType : byte
    {
        NotSet = 0,
        ClientJoinGame,
        SynchronizeClockRequest,
        ClientInGameReady,
        PlayerUpdate,
    }
}
