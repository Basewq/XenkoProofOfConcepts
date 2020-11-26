using MultiplayerExample.Network.NetworkMessages;
using System;
using System.Threading.Tasks;

namespace MultiplayerExample.Network
{
    interface IGameNetworkService
    {
        NetworkGameMode NetworkGameMode { get; }

        bool IsGameHost { get; }

        void StartLocalGame();
        Task<ConnectResult> BeginConnectToServer(string serverIp, ushort serverPortNumber);
        IGameNetworkServerHandler StartHost();
        IGameNetworkServerHandler StartDedicatedServer();

        IGameNetworkClientHandler GetClientHandler();
        IGameNetworkServerHandler GetServerHandler();
    }

    interface IGameNetworkClientHandler
    {
        TimeSpan AverageNetworkLatency { get; }
        void EndConnection();

        Task<JoinGameRequestResult> SendJoinGameRequest(string playerName);
        Task<ClockSyncResult> SendClockSynchronization();

        Task<ClientInGameReadyResult> SendClientInGameReady();
    }

    interface IGameNetworkServerHandler
    {
        void SendMessageToAllPlayers(NetworkMessageWriter message, SendNetworkMessageType sendType);
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

    struct ConnectResult : INetworkMessagingResponse
    {
        public bool IsOk { get; set; }
        public string ErrorMessage { get; set; }

        public IGameNetworkClientHandler ClientHandler { get; set; }
    }

    struct JoinGameRequestResult : INetworkMessagingResponse
    {
        public bool IsOk { get; set; }
        public string ErrorMessage { get; set; }

        public SerializableGuid LoadSceneAssetId;
    }

    struct ClockSyncResult : INetworkMessagingResponse
    {
        public bool IsOk { get; set; }
        public string ErrorMessage { get; set; }
    }

    struct ClientInGameReadyResult : INetworkMessagingResponse
    {
        public bool IsOk { get; set; }
        public string ErrorMessage { get; set; }
    }
}
