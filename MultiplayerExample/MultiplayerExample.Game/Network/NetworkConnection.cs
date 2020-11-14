using LiteNetLib;
using MultiplayerExample.Network.NetworkMessages;
using System;

namespace MultiplayerExample.Network
{
    public readonly struct NetworkConnection : IEquatable<NetworkConnection>
    {
        private readonly NetPeer _netPeer;

        public NetworkConnectionId ConnectionId => _netPeer.Id;

        public bool IsConnected => ((_netPeer?.ConnectionState ?? default) & ConnectionState.Connected) == ConnectionState.Connected;

        public NetworkConnection(NetPeer netPeer)
        {
            _netPeer = netPeer;
        }

        public static implicit operator NetworkConnection(NetPeer peer) => new NetworkConnection(peer);

        public void Send(NetworkMessageWriter message, SendNetworkMessageType sendType)
        {
            _netPeer.Send(message, sendType.ToDeliveryMethod());
        }

        public void Disconnect()
        {
            _netPeer.Disconnect();
        }

        public static bool operator ==(NetworkConnection left, NetworkConnection right) => left.Equals(right);

        public static bool operator !=(NetworkConnection left, NetworkConnection right) => !left.Equals(right);

        public override bool Equals(object obj) => (obj is NetworkConnection val && Equals(val));

        public bool Equals(NetworkConnection other) => _netPeer == other._netPeer;

        public override int GetHashCode() => _netPeer.GetHashCode();
    }
}
