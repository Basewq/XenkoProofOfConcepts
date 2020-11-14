using System;
using System.Diagnostics;

namespace MultiplayerExample.Network
{
    [DebuggerDisplay("{DebugDisplayString,nq}")]
    public readonly struct NetworkConnectionId : IEquatable<NetworkConnectionId>
    {
        private readonly int _value;

        public NetworkConnectionId(int value)
        {
            _value = value;
        }

        internal string DebugDisplayString => _value.ToString();

        public override string ToString() => _value.ToString();

        public static bool operator ==(NetworkConnectionId left, NetworkConnectionId right) => left.Equals(right);

        public static bool operator !=(NetworkConnectionId left, NetworkConnectionId right) => !left.Equals(right);

        public override bool Equals(object obj) => (obj is NetworkConnectionId val && Equals(val));

        public bool Equals(NetworkConnectionId other) => _value.Equals(other._value);

        public override int GetHashCode() => _value.GetHashCode();

        public static implicit operator int(in NetworkConnectionId id) => id._value;
        public static implicit operator NetworkConnectionId(in int value) => new NetworkConnectionId(value);
    }
}
