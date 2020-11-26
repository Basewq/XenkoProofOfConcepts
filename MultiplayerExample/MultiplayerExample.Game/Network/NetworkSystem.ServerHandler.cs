using LiteNetLib;
using LiteNetLib.Utils;
using MultiplayerExample.Network.NetworkMessages;
using MultiplayerExample.Network.NetworkMessages.Client;
using Stride.Core.Collections;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace MultiplayerExample.Network
{
    partial class NetworkSystem : IGameNetworkServerHandler
    {
        void IGameNetworkServerHandler.SendMessageToAllPlayers(NetworkMessageWriter message, SendNetworkMessageType sendType)
        {
            _serverNetworkHandler.SendToAll(message, sendType);
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
    }
}
