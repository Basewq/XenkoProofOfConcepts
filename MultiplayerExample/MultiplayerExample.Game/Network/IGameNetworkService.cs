using MultiplayerExample.Network.NetworkMessages;
using System;
using System.Threading.Tasks;

namespace MultiplayerExample.Network
{
    interface IGameNetworkService
    {
        bool IsEnabled { get; set; }

        NetworkGameMode NetworkGameMode { get; }

        TimeSpan Client_AverageNetworkLatency { get; }

        void StartLocalGame();
        void StartHost();
        Task<ConnectResult> BeginConnectToServer(string serverIp, ushort serverPortNumber);
        void EndConnectionToServer();
        void StartDedicatedServer();

        Task<JoinGameRequestResult> Client_SendJoinGameRequest(string playerName);
        Task<ClockSyncResult> Client_SendClockSynchronization();

        Task<ClientInGameReadyResult> Client_SendClientInGameReady();

        void Server_SendToAll(NetworkMessageWriter message, SendNetworkMessageType sendType);
    }

    enum NetworkGameMode
    {
        NotSet,
        /// <summary>
        /// Game is hosted locally, and does not accept any remote clients.
        /// </summary>
        Local,
        /// <summary>
        /// Game is connected locally, accepts remote clients and also has a local client connected.
        /// </summary>
        ListenServer,
        /// <summary>
        /// Game is hosted locally, accepts remote clients but does not have any locally connected clients.
        /// </summary>
        DedicatedServer,
        /// <summary>
        /// Game is on a remote server, and client is connected to this server.
        /// </summary>
        RemoteClient,
    }

    interface INetworkMessagingResponse
    {
        bool IsOk { get; set; }
        string ErrorMessage { get; set; }
    }

    public struct ConnectResult : INetworkMessagingResponse
    {
        public bool IsOk { get; set; }
        public string ErrorMessage { get; set; }
    }

    public struct JoinGameRequestResult : INetworkMessagingResponse
    {
        public bool IsOk { get; set; }
        public string ErrorMessage { get; set; }

        public SerializableGuid LoadSceneAssetId;
    }

    public struct ClockSyncResult : INetworkMessagingResponse
    {
        public bool IsOk { get; set; }
        public string ErrorMessage { get; set; }
    }

    public struct ClientInGameReadyResult : INetworkMessagingResponse
    {
        public bool IsOk { get; set; }
        public string ErrorMessage { get; set; }
    }
}
