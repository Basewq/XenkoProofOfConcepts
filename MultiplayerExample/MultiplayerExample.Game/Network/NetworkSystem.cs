using LiteNetLib;
using LiteNetLib.Utils;
using MultiplayerExample.Core;
using MultiplayerExample.Network.NetworkMessages;
using MultiplayerExample.Network.NetworkMessages.Client;
using MultiplayerExample.Network.NetworkMessages.Server;
using MultiplayerExample.Utilities;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Engine;
using Stride.Games;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MultiplayerExample.Network
{
    class NetworkSystem : GameSystemBase, IGameNetworkService
    {
        private const int ConnectionTimeoutInMilliseconds = 30 * 1000;

        public const string ServerConnectionAppName = "MultiplayerExample";
        public const ushort ServerPortNumber = 60000;   // TODO: should probably be a config setting

        private ServerNetworkHandler _serverNetworkHandler;

        private ClientNetworkHandler _clientNetworkHandler;

        private SceneSystem _sceneSystem;
        private SceneInstance _currentSceneInstance;
        private NetworkEntityProcessor _networkEntityProcessor;
        private GameClockManager _gameClockManager;

        public bool IsEnabled { get; set; } = true;

        public NetworkGameMode NetworkGameMode { get; private set; }

        public TimeSpan Client_AverageNetworkLatency => _clientNetworkHandler.AverageNetworkLatency;

        public NetworkSystem([NotNull] IServiceRegistry registry) : base(registry)
        {
            Enabled = true;
            Services.AddService<IGameNetworkService>(this);
            _serverNetworkHandler = new ServerNetworkHandler(this);
            _clientNetworkHandler = new ClientNetworkHandler(this);
        }

        public override void Initialize()
        {
            _sceneSystem = Services.GetSafeServiceAs<SceneSystem>();
            _gameClockManager = Services.GetSafeServiceAs<GameClockManager>();
        }

        /// <summary>
        /// Start this game with only a local client.
        /// </summary>
        public void StartLocalGame()
        {
            Debug.Assert(NetworkGameMode == NetworkGameMode.NotSet);
            NetworkGameMode = NetworkGameMode.Local;
            // TODO: Need to implement this for local only game.
            throw new Exception("Not implemented");
        }

        /// <summary>
        /// Start this game with a local client and accepts remote clients.
        /// </summary>
        public void StartHost()
        {
            Debug.Assert(NetworkGameMode == NetworkGameMode.NotSet);
            NetworkGameMode = NetworkGameMode.ListenServer;
            _serverNetworkHandler.Start();
        }

        /// <summary>
        /// Join a remote game.
        /// </summary>
        public Task<ConnectResult> BeginConnectToServer(string serverIp, ushort serverPortNumber)
        {
            Debug.Assert(NetworkGameMode == NetworkGameMode.NotSet);
            NetworkGameMode = NetworkGameMode.RemoteClient;
            return _clientNetworkHandler.Connect(serverIp, serverPortNumber);
        }

        public void EndConnectionToServer()
        {
            _clientNetworkHandler.Disconnect();
        }

        /// <summary>
        /// Start this game as a dedicated server.
        /// </summary>
        public void StartDedicatedServer()
        {
            Debug.Assert(NetworkGameMode == NetworkGameMode.NotSet);
            NetworkGameMode = NetworkGameMode.DedicatedServer;
            _serverNetworkHandler.Start();
        }

        Task<JoinGameRequestResult> IGameNetworkService.Client_SendJoinGameRequest(string playerName)
        {
            return _clientNetworkHandler.SendJoinGame(playerName);
        }

        Task<ClockSyncResult> IGameNetworkService.Client_SendClockSynchronization()
        {
            return _clientNetworkHandler.SendClockSynchronization();
        }

        Task<ClientInGameReadyResult> IGameNetworkService.Client_SendClientInGameReady()
        {
            return _clientNetworkHandler.SendClientInGameReady();
        }

        void IGameNetworkService.Server_SendToAll(NetworkMessageWriter message, SendNetworkMessageType sendType)
        {
            _serverNetworkHandler.SendToAll(message, sendType);
        }

        public override void Update(GameTime gameTime)
        {
            if (_currentSceneInstance != _sceneSystem?.SceneInstance)
            {
                UpdateCurrentSceneInstance(_sceneSystem.SceneInstance);
            }

            if (NetworkGameMode == NetworkGameMode.NotSet)
            {
                // Do nothing
                return;
            }

            switch (NetworkGameMode)
            {
                case NetworkGameMode.Local:
                    //??_serverNetworkHandler.Update();
                    break;
                case NetworkGameMode.RemoteClient:
                    _clientNetworkHandler.Update(gameTime);
                    break;
                case NetworkGameMode.ListenServer:
                    _serverNetworkHandler.Update();
                    _clientNetworkHandler.Update(gameTime);
                    break;
                case NetworkGameMode.DedicatedServer:
                    _serverNetworkHandler.Update();
                    break;

                default:
                    Debug.Fail($"Unhandled NetworkGameMode: {NetworkGameMode}");
                    break;
            }
        }

        private void UpdateCurrentSceneInstance(SceneInstance newSceneInstance)
        {
            //if (_currentSceneInstance != null && _networkEntityProcessor != null)
            //{
            //    _currentSceneInstance.Processors.Remove(_networkEntityProcessor);
            //}

            // Set the current scene
            _currentSceneInstance = newSceneInstance;

            if (_currentSceneInstance != null && !_currentSceneInstance.Processors.Any(x => x is NetworkEntityProcessor))
            {
                // Create this NetworkEntityProcessor if it doesn't exist
                _networkEntityProcessor = new NetworkEntityProcessor();
                _currentSceneInstance.Processors.Add(_networkEntityProcessor);
            }
        }

        [Conditional("DEBUG")]
        private void DebugWriteLine(string message)
        {
            message = $"[{nameof(NetworkSystem)}] " + message;
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }

        private void OnClientDisconnected()
        {
            switch (NetworkGameMode)
            {
                case NetworkGameMode.RemoteClient:
                case NetworkGameMode.ListenServer:
                    //_clientNetworkHandler

                    ///???? Deactivate game somehow?
                    break;

                default:
                    Debug.Fail($"Invalid NetworkGameMode state: {NetworkGameMode}");
                    break;
            }
            NetworkGameMode = NetworkGameMode.NotSet;
        }

        private class ServerNetworkHandler : INetEventListener
        {
            private readonly NetworkSystem _networkSystem;
            private readonly NetManager _netManager;
            private readonly NetworkMessageWriter _networkMessageWriter;

            private readonly FastList<ClientConnectionDetails> _activeClients = new FastList<ClientConnectionDetails>();

            private readonly FastList<PlayerUpdateInputMessage> _pendingPlayerUpdateInputMessages = new FastList<PlayerUpdateInputMessage>();

            public ServerNetworkHandler(NetworkSystem networkSystem)
            {
                _networkSystem = networkSystem;
                _netManager = new NetManager(this)
                {
                    AutoRecycle = true,
                    DisconnectTimeout = ConnectionTimeoutInMilliseconds,
                };
                _networkMessageWriter = new NetworkMessageWriter(new NetDataWriter());
            }

            internal void Start()
            {
#if DEBUG
                //_netManager.SimulatePacketLoss = true;
                //_netManager.SimulationPacketLossChance = 10;
                //_netManager.SimulateLatency = true;
                //_netManager.SimulationMinLatency = 100;
                //_netManager.SimulationMaxLatency = 150;
#endif

                _netManager.Start(ServerPortNumber);
                _networkSystem.DebugWriteLine($"{nameof(NetworkSystem)} begin listening on port {_netManager.LocalPort}...");

            }

            internal void Update()
            {
                _netManager.PollEvents();
            }

            internal void SendToAll(NetworkMessageWriter message, SendNetworkMessageType sendType)
            {
                _netManager.SendToAll(message, sendType.ToDeliveryMethod());
            }

            void INetEventListener.OnConnectionRequest(ConnectionRequest request)
            {
                const int MaxPlayers = 4;
                if (_netManager.ConnectedPeersCount < MaxPlayers)
                {
                    request.AcceptIfKey(ServerConnectionAppName);
                }
                else
                {
                    request.Reject();
                }
            }

            void INetEventListener.OnPeerConnected(NetPeer peer)
            {
                _networkSystem.DebugWriteLine($"Svr Player connected: {peer.EndPoint}");
                // Do nothing. Wait for the player to initiate requests.
            }

            void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
            {
                _networkSystem.DebugWriteLine($"Svr OnPeerDisconnected: {disconnectInfo.Reason}");

                var networkEntityProcessor = _networkSystem._networkEntityProcessor;
                bool wasRemoved = false;
                for (int i = 0; i < _activeClients.Count; i++)
                {
                    var clientDetails = _activeClients[i];
                    if (clientDetails.Connection.ConnectionId != peer.Id)
                    {
                        continue;
                    }
                    _activeClients.RemoveAt(i);
                    networkEntityProcessor.Server_RemovePlayer(clientDetails.PlayerId);
                    _networkSystem.DebugWriteLine($"Svr Player disconnected: { clientDetails.PlayerName}");
                    wasRemoved = true;
                    break;
                }

                if (!wasRemoved)
                {
                    _networkSystem.DebugWriteLine($"Svr Player disconnected, but not found in active list.");
                }
            }

            void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
            {
                _networkSystem.DebugWriteLine($"Svr OnNetworkError: {socketError}");
            }

            void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
            {
                var message = new NetworkMessageReader(reader);
                if (!message.ReadEnumFromByte<ClientMessageType>(out var messageType))
                {
                    Debug.Fail($"Svr Could not read message type.");
                    return;
                }
                var networkEntityProcessor = _networkSystem._networkEntityProcessor;
                switch (messageType)
                {
                    case ClientMessageType.ClientJoinGame:
                        {
                            _networkSystem.DebugWriteLine($"Svr ProcessData message type: {messageType}");
                            ClientJoinGameRequestMessage joinGameMsg = default;
                            if (joinGameMsg.TryRead(message))
                            {
                                _networkSystem.DebugWriteLine($"Svr Player connected: {joinGameMsg.PlayerName}");
                                var clientDetails = new ClientConnectionDetails
                                {
                                    PlayerId = Guid.NewGuid(),      // TODO: should keep track of used guids on the off-chance we gete id collisions
                                    PlayerName = joinGameMsg.PlayerName,
                                    Connection = peer,
                                };
                                _activeClients.Add(clientDetails);

                                Debug.Assert(networkEntityProcessor != null, "NetworkEntityProcessor has not been registered.");
                                networkEntityProcessor.Server_AddPlayer(clientDetails.PlayerId, clientDetails.PlayerName, clientDetails.Connection);
                            }
                        }
                        break;

                    case ClientMessageType.SynchronizeClockRequest:
                        {
                            _networkSystem.DebugWriteLine($"Svr ProcessData message type: {messageType}");
                            SynchronizeClockRequestMessage syncClockRequestMsg = default;
                            if (syncClockRequestMsg.TryRead(message))
                            {
                                int clientIndex = FindClientDetailsIndex(peer);
                                if (clientIndex >= 0)
                                {
                                    ref var clientDetails = ref _activeClients.Items[clientIndex];
                                    networkEntityProcessor.Server_SendSynchronizeClockResponse(clientDetails.PlayerId, syncClockRequestMsg);
                                }
                            }
                        }
                        break;

                    case ClientMessageType.ClientInGameReady:
                        {
                            _networkSystem.DebugWriteLine($"Svr ProcessData message type: {messageType}");
                            ClientInGameReadyMessage clientInGameReadyMsg = default;
                            if (clientInGameReadyMsg.TryRead(message))
                            {
                                int clientIndex = FindClientDetailsIndex(peer);
                                if (clientIndex >= 0)
                                {
                                    ref var clientDetails = ref _activeClients.Items[clientIndex];
                                    if (!clientDetails.IsInGame)
                                    {
                                        clientDetails.IsInGame = true;
                                        networkEntityProcessor.Server_SetPlayerReady(clientDetails.PlayerId);
                                    }
                                }
                            }
                        }
                        break;

                    case ClientMessageType.PlayerUpdate:
                        {
                            //WriteLine($"Svr ProcessData message type: {messageType}");
                            int clientIndex = FindClientDetailsIndex(peer);
                            if (clientIndex >= 0)
                            {
                                ref var clientDetails = ref _activeClients.Items[clientIndex];
                                PlayerUpdateMessage playerUpdateMsg = default;
                                if (playerUpdateMsg.TryRead(message))
                                {
                                    PlayerUpdateInputMessage playerUpdateInputMsg = default;
                                    if (playerUpdateInputMsg.TryReadHeader(message, out var inputCount))
                                    {
                                        if (inputCount > 0)
                                        {
                                            _pendingPlayerUpdateInputMessages.Clear();
                                            _pendingPlayerUpdateInputMessages.EnsureCapacity(inputCount);
                                            for (int i = 0; i < inputCount; i++)
                                            {
                                                if (playerUpdateInputMsg.TryReadNextArrayItem(message))
                                                {
                                                    _pendingPlayerUpdateInputMessages.Add(playerUpdateInputMsg);
                                                }
                                            }

                                            networkEntityProcessor.Server_CollectPendingInputs(clientDetails.PlayerId, playerUpdateMsg, _pendingPlayerUpdateInputMessages);
                                        }
                                    }
                                }
                            }
                        }
                        break;

                    default:
                        Debug.Fail($"Unhandled message type: {messageType}");
                        break;
                }
            }

            void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
            {
                _networkSystem.DebugWriteLine($"Svr Unhandled method: OnNetworkReceiveUnconnected");
            }

            void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latencyInMilliseconds)
            {
                int clientIndex = FindClientDetailsIndex(peer);
                if (clientIndex >= 0)
                {
                    ref var clientDetails = ref _activeClients.Items[clientIndex];
                    clientDetails.AverageNetworkLatency = TimeSpan.FromMilliseconds(latencyInMilliseconds);
                }
            }

            private int FindClientDetailsIndex(NetPeer peer)
            {
                var connection = new NetworkConnection(peer);
                for (int i = 0; i < _activeClients.Count; i++)
                {
                    if (_activeClients.Items[i].Connection == connection)
                    {
                        return i;
                    }
                }
                return -1;
            }
        }

        private class ClientNetworkHandler : INetEventListener
        {
            private bool _isConnected = false;
            private NetworkConnection _connectionToServer;
            private TaskCompletionSource<ConnectResult> _pendingConnectionTaskCompletionSource;

            private TaskCompletionSource<ClockSyncResult> _clockSyncTaskCompletionSource;
            private bool _isSyncingClock = false;
            private const int SendSyncClockRetryCount = 5;
            private TimeSpan _syncClockTimeoutRemaining = TimeSpan.Zero;
            private int _syncClockRetryCountRemaining = 5;

            private TaskCompletionSource<JoinGameRequestResult> _joinGameTaskCompletionSource;

            private TaskCompletionSource<ClientInGameReadyResult> _inGameReadyTaskCompletionSource;

            private readonly NetworkSystem _networkSystem;
            private readonly NetManager _netManager;
            private readonly NetworkMessageWriter _networkMessageWriter;

            private GameTime _currentGameTime;  // Only valid during an Update

            public TimeSpan AverageNetworkLatency { get; private set; }

            public bool IsConnected => _isConnected;

            public ClientNetworkHandler(NetworkSystem networkSystem)
            {
                _networkSystem = networkSystem;
                _netManager = new NetManager(this)
                {
                    AutoRecycle = true,
                    DisconnectTimeout = ConnectionTimeoutInMilliseconds,
                };
                _networkMessageWriter = new NetworkMessageWriter(new NetDataWriter());
            }

            internal Task<ConnectResult> Connect(string serverIpAddress, ushort serverPortNumber)
            {
#if DEBUG
                //_netManager.SimulatePacketLoss = true;
                //_netManager.SimulationPacketLossChance = 10;
                //_netManager.SimulateLatency = true;
                //_netManager.SimulationMinLatency = 100;
                //_netManager.SimulationMaxLatency = 150;
#endif
                _netManager.Start();
                _netManager.Connect(serverIpAddress, serverPortNumber, ServerConnectionAppName);

                // TODO: should validate if already connecting or not
                _pendingConnectionTaskCompletionSource = new TaskCompletionSource<ConnectResult>();
                return _pendingConnectionTaskCompletionSource.Task;
            }

            internal void Disconnect()
            {
                Debug.Assert(_connectionToServer != null);
                _connectionToServer.Disconnect();
                _netManager.Stop();
            }

            internal void Update(GameTime gameTime)
            {
                _currentGameTime = gameTime;
                _netManager.PollEvents();
                _currentGameTime = null;

                if (_isSyncingClock)
                {
                    _syncClockTimeoutRemaining -= gameTime.Elapsed;
                    if (_syncClockTimeoutRemaining.Ticks <= 0)
                    {
                        if (_syncClockRetryCountRemaining > 0)
                        {
                            _syncClockRetryCountRemaining--;
                            if (!SendSyncClockMessage(_syncClockRetryCountRemaining))
                            {
                                var errMsg = "Clock sync failed: Disconnected from server.";
                                SetTaskFailureIfExists(ref _clockSyncTaskCompletionSource, errMsg);
                            }
                        }
                        else
                        {
                            _isSyncingClock = false;

                            Debug.Assert(_clockSyncTaskCompletionSource != null);
                            var errMsg = "Clock sync has timed out.";
                            SetTaskFailureIfExists(ref _clockSyncTaskCompletionSource, errMsg);
                        }
                    }
                }
            }

            internal Task<JoinGameRequestResult> SendJoinGame(string playerName)
            {
                Debug.Assert(_connectionToServer.IsConnected);

                _networkMessageWriter.Reset();
                var connectMsg = new ClientJoinGameRequestMessage(playerName);     // Should probably send something more secure...
                connectMsg.WriteTo(_networkMessageWriter);
                _connectionToServer.Send(_networkMessageWriter, SendNetworkMessageType.ReliableOrdered);

                Debug.Assert(_joinGameTaskCompletionSource == null);
                _joinGameTaskCompletionSource = new TaskCompletionSource<JoinGameRequestResult>();
                return _joinGameTaskCompletionSource.Task;
            }

            internal Task<ClockSyncResult> SendClockSynchronization()
            {
                Debug.Assert(_clockSyncTaskCompletionSource == null);
                _clockSyncTaskCompletionSource = new TaskCompletionSource<ClockSyncResult>();

                if (SendSyncClockMessage(SendSyncClockRetryCount))
                {
                    _isSyncingClock = true;
                    return _clockSyncTaskCompletionSource.Task;
                }
                else
                {
                    _isSyncingClock = false;
                    var resultTask = _clockSyncTaskCompletionSource.Task;
                    var errMsg = "Clock sync failed: Disconnected from server.";
                    SetTaskFailureIfExists(ref _clockSyncTaskCompletionSource, errMsg);
                    return resultTask;
                }
            }

            private bool SendSyncClockMessage(int nextRetryCountRemaining)
            {
                if (!_connectionToServer.IsConnected)
                {
                    return false;
                }

                _networkMessageWriter.Reset();
                var syncClockRequestMsg = new SynchronizeClockRequestMessage
                {
                    ClientOSTimestamp = TimeExt.GetOSTimestamp()
                };
                syncClockRequestMsg.WriteTo(_networkMessageWriter);
                _connectionToServer.Send(_networkMessageWriter, SendNetworkMessageType.Unreliable);

                _syncClockTimeoutRemaining = TimeSpan.FromSeconds(3);
                _syncClockRetryCountRemaining = nextRetryCountRemaining;
                return true;
            }

            internal Task<ClientInGameReadyResult> SendClientInGameReady()
            {
                Debug.Assert(_connectionToServer.IsConnected);
                _networkMessageWriter.Reset();
                var clientReadyMsg = new ClientInGameReadyMessage();
                clientReadyMsg.WriteTo(_networkMessageWriter);
                _connectionToServer.Send(_networkMessageWriter, SendNetworkMessageType.ReliableOrdered);

                Debug.Assert(_inGameReadyTaskCompletionSource == null);
                _inGameReadyTaskCompletionSource = new TaskCompletionSource<ClientInGameReadyResult>();
                return _inGameReadyTaskCompletionSource.Task;
            }

            void INetEventListener.OnConnectionRequest(ConnectionRequest request)
            {
                request.Reject();   // This is not a server
            }

            void INetEventListener.OnPeerConnected(NetPeer peer)
            {
                _networkSystem.DebugWriteLine($"Cln Player connected: {peer.EndPoint}");

                _connectionToServer = new NetworkConnection(peer);
                _isConnected = true;

                Debug.Assert(_pendingConnectionTaskCompletionSource != null);
                var connResult = new ConnectResult
                {
                    IsOk = true
                };
                _pendingConnectionTaskCompletionSource.SetResult(connResult);
                _pendingConnectionTaskCompletionSource = null;
            }

            void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
            {
                _networkSystem.DebugWriteLine($"Cln OnPeerDisconnected: {disconnectInfo.Reason}");

                SetTaskFailureIfExists(ref _pendingConnectionTaskCompletionSource, "Could not connect to server.");
                var errMsg = "Disconnected from server.";
                SetTaskFailureIfExists(ref _clockSyncTaskCompletionSource, errMsg);
                SetTaskFailureIfExists(ref _joinGameTaskCompletionSource, errMsg);
                SetTaskFailureIfExists(ref _inGameReadyTaskCompletionSource, errMsg);

                _isConnected = false;
                _connectionToServer = default;
                _networkSystem.OnClientDisconnected();

                // TODO: need to stop and return to main screen...
            }

            void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
            {
                _networkSystem.DebugWriteLine($"Cln OnNetworkError: {socketError}");
            }

            void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
            {
                var message = new NetworkMessageReader(reader);
                if (!message.ReadEnumFromByte<ServerMessageType>(out var messageType))
                {
                    Debug.Fail($"Cln Could not read message type.");
                    return;
                }
                switch (messageType)
                {
                    case ServerMessageType.ClientJoinGameResponse:
                        Debug.WriteLine($"Cln ProcessData message type: {messageType}");
                        ProcessMessageClientJoinGameResponse(message);
                        break;
                    case ServerMessageType.SynchronizeClockResponse:
                        Debug.WriteLine($"Cln ProcessData message type: {messageType}");
                        Debug.Assert(_currentGameTime != null);
                        ProcessMessageSynchronizeClockResponse(message, _currentGameTime);
                        break;

                    case ServerMessageType.SpawnLocalPlayer:
                        Debug.WriteLine($"Cln ProcessData message type: {messageType}");
                        ProcessMessageSpawnLocalPlayer(message, peer);
                        break;

                    case ServerMessageType.SpawnRemotePlayer:
                        Debug.WriteLine($"Cln ProcessData message type: {messageType}");
                        ProcessMessageSpawnRemotePlayer(message);
                        break;

                    case ServerMessageType.DespawnRemotePlayer:
                        Debug.WriteLine($"Cln ProcessData message type: {messageType}");
                        ProcessMessageDespawnPlayer(message);
                        break;

                    case ServerMessageType.SnaphotUpdates:
                        //Debug.WriteLine($"Cln ProcessData message type: {messageType}");
                        Debug.Assert(_currentGameTime != null);
                        ProcessMessageSnapshotUpdates(message, _currentGameTime);
                        break;

                    default:
                        Debug.Fail($"Cln Unhandled message type: {messageType}");
                        break;
                }
            }

            void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
            {
                _networkSystem.DebugWriteLine($"Cln Unhandled method: OnNetworkReceiveUnconnected");
            }

            void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latencyInMilliseconds)
            {
                AverageNetworkLatency = TimeSpan.FromMilliseconds(latencyInMilliseconds);
            }

            private void ProcessMessageClientJoinGameResponse(NetworkMessageReader message)
            {
                ClientJoinGameResponseMessage joinGameResponseMsg = default;
                if (joinGameResponseMsg.TryRead(message))
                {
                    Debug.WriteLine($"Cln Player connection response - PlayerId: {joinGameResponseMsg.CanJoinGame}");

                    if (_joinGameTaskCompletionSource == null) return;
                    Debug.Assert(_joinGameTaskCompletionSource != null);
                    var joinGameResult = new JoinGameRequestResult
                    {
                        IsOk = joinGameResponseMsg.CanJoinGame,
                        LoadSceneAssetId = joinGameResponseMsg.InGameSceneAssetId,
                    };
                    _joinGameTaskCompletionSource.SetResult(joinGameResult);
                    _joinGameTaskCompletionSource = null;
                }
                else
                {
                    Debug.Assert(_joinGameTaskCompletionSource != null);
                    var errMsg = "Failed to read join game response message.";
                    SetTaskFailureIfExists(ref _joinGameTaskCompletionSource, errMsg);
                }
            }

            private void ProcessMessageSynchronizeClockResponse(NetworkMessageReader message, GameTime gameTime)
            {
                if (!_isSyncingClock)
                {
                    // Old message, don't process
                    return;
                }

                SynchronizeClockResponseMessage msg = default;
                if (msg.TryRead(message))
                {
                    var gameClockManager = _networkSystem._gameClockManager;

                    var svrSimTime = TimeSpan.FromTicks(msg.ServerWorldTimeInTicks);
                    var svrSimTickNo = (SimulationTickNumber)(msg.ServerWorldTimeInTicks / GameConfig.PhysicsFixedTimeStep.Ticks);
                    gameClockManager.NetworkServerSimulationClock.TargetTotalTime = svrSimTime;
                    gameClockManager.NetworkServerSimulationClock.LastServerSimulationTickNumber = svrSimTickNo;
                    gameClockManager.NetworkServerSimulationClock.CurrentTickTimeElapsed = GameClockManager.CalculateTickTimeElapsed(svrSimTime, svrSimTickNo);
                    gameClockManager.NetworkServerSimulationClock.IsEnabled = true;

                    // Set this immediately to the client's clock
                    gameClockManager.SimulationClock.Reset();
                    gameClockManager.SimulationClock.TotalTime = svrSimTime;
                    gameClockManager.SimulationClock.SimulationTickNumber = svrSimTickNo;
                    gameClockManager.SimulationClock.IsEnabled = true;
                    Debug.WriteLine($"Cln SyncClock TickNo: {gameClockManager.SimulationClock.SimulationTickNumber}");

                    // Finished syncing the clock
                    _isSyncingClock = false;

                    Debug.Assert(_clockSyncTaskCompletionSource != null);
                    var clockSyncResult = new ClockSyncResult
                    {
                        IsOk = true
                    };
                    _clockSyncTaskCompletionSource.SetResult(clockSyncResult);
                    _clockSyncTaskCompletionSource = null;
                }
            }

            private void ProcessMessageSpawnLocalPlayer(NetworkMessageReader message, NetworkConnection connectionToServer)
            {
                var networkEntityProcessor = _networkSystem._networkEntityProcessor;
                networkEntityProcessor.Client_SpawnLocalPlayer(message, connectionToServer);
            }

            private void ProcessMessageSpawnRemotePlayer(NetworkMessageReader message)
            {
                var networkEntityProcessor = _networkSystem._networkEntityProcessor;
                networkEntityProcessor.Client_SpawnRemotePlayer(message);
            }

            private void ProcessMessageDespawnPlayer(NetworkMessageReader message)
            {
                var networkEntityProcessor = _networkSystem._networkEntityProcessor;
                networkEntityProcessor.Client_DespawnRemotePlayer(message);
            }

            private void ProcessMessageSnapshotUpdates(NetworkMessageReader message, GameTime gameTime)
            {
                var networkEntityProcessor = _networkSystem._networkEntityProcessor;
                networkEntityProcessor.Client_UpdateStates(message);
            }

            private void SetTaskFailureIfExists<T>(ref TaskCompletionSource<T> taskCompletionSource, string errorMessage)
                where T : struct, INetworkMessagingResponse
            {
                if (taskCompletionSource == null)
                {
                    return;
                }
                var taskResult = new T
                {
                    IsOk = false,
                    ErrorMessage = errorMessage
                };
                taskCompletionSource.SetResult(taskResult);
                taskCompletionSource = null;
            }
        }

        private struct ClientConnectionDetails
        {
            /// <summary>
            /// This is the same as the NetworkEntityId.
            /// </summary>
            internal SerializableGuid PlayerId;
            internal string PlayerName;
            internal NetworkConnection Connection;
            internal bool IsInGame;
            internal TimeSpan AverageNetworkLatency;
        }
    }
}
