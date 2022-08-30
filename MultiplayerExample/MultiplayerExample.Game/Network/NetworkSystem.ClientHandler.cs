using LiteNetLib;
using LiteNetLib.Utils;
using MultiplayerExample.Core;
using MultiplayerExample.Network.EntityMessages;
using MultiplayerExample.Network.NetworkMessages;
using MultiplayerExample.Network.NetworkMessages.Client;
using MultiplayerExample.Network.NetworkMessages.Server;
using MultiplayerExample.Utilities;
using Stride.Core.Collections;
using Stride.Games;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MultiplayerExample.Network
{
    partial class NetworkSystem : IGameNetworkClientHandler
    {
        private event Action _networkClientDisconnected;
        event Action IGameNetworkClientHandler.Disconnected
        {
            add => _networkClientDisconnected += value;
            remove => _networkClientDisconnected -= value;
        }

        TimeSpan IGameNetworkClientHandler.AverageNetworkLatency => _clientNetworkHandler.AverageNetworkLatency;

        void IGameNetworkClientHandler.EndConnection()
        {
            _clientNetworkHandler.Disconnect();
        }

        Task<JoinGameRequestResult> IGameNetworkClientHandler.SendJoinGameRequest(string playerName)
        {
            return _clientNetworkHandler.SendJoinGame(playerName);
        }

        Task<ClockSyncResult> IGameNetworkClientHandler.SendClockSynchronization()
        {
            return _clientNetworkHandler.SendClockSynchronization();
        }

        Task<ClientInGameReadyResult> IGameNetworkClientHandler.SendClientInGameReady()
        {
            return _clientNetworkHandler.SendClientInGameReady();
        }

        private void OnClientDisconnected()
        {
            switch (NetworkGameMode)
            {
                case NetworkGameMode.RemoteClient:
                case NetworkGameMode.ListenServer:
                    _networkClientDisconnected?.Invoke();
                    break;

                default:
                    Debug.Fail($"Invalid NetworkGameMode state: {NetworkGameMode}");
                    break;
            }
            NetworkGameMode = NetworkGameMode.NotSet;
        }

        private class ClientNetworkHandler : INetEventListener
        {
            private NetworkConnection _connectionToServer;
            private TaskCompletionSource<ConnectResult> _pendingConnectionTaskCompletionSource;

            private TaskCompletionSource<ClockSyncResult> _clockSyncTaskCompletionSource;
            private bool _isSyncingClock = false;
            private const int SendSyncClockRetryCount = 5;
            private TimeSpan _syncClockTimeoutRemaining = TimeSpan.Zero;
            private int _syncClockRetryCountRemaining = 5;

            private TaskCompletionSource<JoinGameRequestResult> _joinGameTaskCompletionSource;

            private readonly NetworkSystem _networkSystem;
            private readonly NetManager _netManager;
            private readonly NetworkMessageWriter _networkMessageWriter;

            private GameTime _currentGameTime;  // Only valid during an Update

            private readonly FastList<EntityUpdateTransform> _updateEntityTransforms = new FastList<EntityUpdateTransform>();
            private readonly FastList<EntityUpdateInputAction> _updateEntityInputs = new FastList<EntityUpdateInputAction>();

            /// <summary>
            /// Average time a packet will take from client to server, ~1/2 round-trip time.
            /// </summary>
            public TimeSpan AverageNetworkLatency { get; private set; }

            public bool IsConnected { get; private set; } = false;

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

                // Not sure if a direct reponse is required, currently we'll just accept that it's sent...
                return Task.FromResult(new ClientInGameReadyResult
                {
                    IsOk = true
                });
            }

            void INetEventListener.OnConnectionRequest(ConnectionRequest request)
            {
                request.Reject();   // This is not a server
            }

            void INetEventListener.OnPeerConnected(NetPeer peer)
            {
                _networkSystem.DebugWriteLine($"Cln Player connected: {peer.EndPoint}");

                _connectionToServer = new NetworkConnection(peer);
                IsConnected = true;

                Debug.Assert(_pendingConnectionTaskCompletionSource != null);
                var connResult = new ConnectResult
                {
                    IsOk = true,
                    ClientHandler = _networkSystem
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

                var networkEntityProcessor = _networkSystem._networkEntityProcessor;
                var clientPlayerManager = networkEntityProcessor.GetClientPlayerManager();
                clientPlayerManager.DespawnAllLocalPlayers();

                IsConnected = false;
                _connectionToServer = default;
                _networkSystem.OnClientDisconnected();

                var gameClockManager = _networkSystem._gameClockManager;
                gameClockManager.SimulationClock.IsEnabled = false;
                // TODO: need to stop and return to main screen...
            }

            void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
            {
                _networkSystem.DebugWriteLine($"Cln OnNetworkError: {socketError}");
            }


            void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
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
                        ProcessMessageSnapshotUpdates(message);
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
                SpawnLocalPlayerMessage msg = default;
                if (msg.TryRead(message))
                {
                    var networkEntityProcessor = _networkSystem._networkEntityProcessor;
                    var clientPlayerManager = networkEntityProcessor.GetClientPlayerManager();
                    clientPlayerManager.SpawnLocalPlayer(msg, connectionToServer);
                }
            }

            private void ProcessMessageSpawnRemotePlayer(NetworkMessageReader message)
            {
                SpawnRemotePlayerMessage msg = default;
                if (msg.TryRead(message))
                {
                    var networkEntityProcessor = _networkSystem._networkEntityProcessor;
                    var clientPlayerManager = networkEntityProcessor.GetClientPlayerManager();
                    clientPlayerManager.SpawnRemotePlayer(msg);
                }
            }

            private void ProcessMessageDespawnPlayer(NetworkMessageReader message)
            {
                DespawnRemotePlayerMessage msg = default;
                if (msg.TryRead(message))
                {
                    var networkEntityProcessor = _networkSystem._networkEntityProcessor;
                    var clientPlayerManager = networkEntityProcessor.GetClientPlayerManager();
                    clientPlayerManager.DespawnRemotePlayer(msg);
                }
            }

            private void ProcessMessageSnapshotUpdates(NetworkMessageReader message)
            {
                EntitySnaphotUpdatesMessage msg = default;
                if (msg.TryRead(message))
                {
                    var networkEntityProcessor = _networkSystem._networkEntityProcessor;
                    var clientPlayerManager = networkEntityProcessor.GetClientPlayerManager();
                    var gameClockManager = _networkSystem._gameClockManager;

                    var hasServerAppliedNewPlayerInput = clientPlayerManager.AcknowledgeReceivedPlayerInputs(msg.AcknowledgedLastReceivedPlayerInputSequenceNumber, msg.LastAppliedServerPlayerInputSequenceNumber);
                    gameClockManager.NetworkServerSimulationClock.SetClockFromTickNumber(msg.ServerSimulationTickNumber);

                    _updateEntityTransforms.Clear();
                    _updateEntityInputs.Clear();
                    PopulateMessages(message, _updateEntityTransforms);
                    PopulateMessages(message, _updateEntityInputs);

                    if (_updateEntityTransforms.Count > 0 || _updateEntityInputs.Count > 0)
                    {
                        //var simTickNumber = msg.ServerSimulationTickNumber;     // TODO: Should be gameclock sim tick???
                        var simTickNumber = gameClockManager.SimulationClock.SimulationTickNumber;
                        clientPlayerManager.UpdateEntityStates(
                            simTickNumber, hasServerAppliedNewPlayerInput, _updateEntityTransforms, _updateEntityInputs, msg.LastAppliedServerPlayerInputSequenceNumber);
                    }

                    //Debug.WriteLine($"Cln ProcessData.ProcessMessageSnapshotUpdates: {_workingUpdateEntityTransforms.Count}");
                }

                static bool PopulateMessages<TMsg>(NetworkMessageReader netMessage, FastList<TMsg> messageList)
                    where TMsg : struct, INetworkMessageArray
                {
                    messageList.Clear();
                    TMsg msg = default;
                    if (!msg.TryReadHeader(netMessage, out var msgCount))
                    {
                        return false;
                    }
                    for (int i = 0; i < msgCount; i++)
                    {
                        if (!msg.TryReadNextArrayItem(netMessage))
                        {
                            continue;
                        }
                        messageList.Add(msg);
                    }
                    return true;
                }
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
