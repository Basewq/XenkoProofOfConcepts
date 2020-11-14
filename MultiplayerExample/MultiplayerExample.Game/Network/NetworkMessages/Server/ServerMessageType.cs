namespace MultiplayerExample.Network.NetworkMessages.Server
{
    enum ServerMessageType : byte
    {
        Unknown = 0,

        ClientJoinGameResponse,
        SynchronizeClockResponse,

        SpawnLocalPlayer,
        SpawnRemotePlayer,
        DespawnRemotePlayer,
        SnaphotUpdates,
    }
}
