using System;
using System.Diagnostics;

namespace MultiplayerExample.Network
{
    [DebuggerDisplay("{DebugDisplayString,nq}")]
    public readonly struct PlayerInputSequenceNumber : IEquatable<PlayerInputSequenceNumber>, IComparable<PlayerInputSequenceNumber>
    {
        private readonly uint _value;

        internal string DebugDisplayString => _value.ToString();

        public PlayerInputSequenceNumber(uint value)
        {
            _value = value;
        }

        public override string ToString() => _value.ToString();

        public static bool operator ==(PlayerInputSequenceNumber left, PlayerInputSequenceNumber right) => left.Equals(right);

        public static bool operator !=(PlayerInputSequenceNumber left, PlayerInputSequenceNumber right) => !left.Equals(right);

        public override bool Equals(object obj) => (obj is PlayerInputSequenceNumber val && Equals(val));

        public bool Equals(PlayerInputSequenceNumber other) => _value.Equals(other._value);

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(PlayerInputSequenceNumber other) => _value.CompareTo(other._value);

        public static implicit operator uint(PlayerInputSequenceNumber serializableuint) => serializableuint._value;
        public static explicit operator PlayerInputSequenceNumber(uint value) => new PlayerInputSequenceNumber(value);

        public static PlayerInputSequenceNumber operator +(PlayerInputSequenceNumber value) => value;
        //public static PlayerInputSequenceNumber operator -(PlayerInputSequenceNumber value) => new PlayerInputSequenceNumber(-value._value);

        public static PlayerInputSequenceNumber operator +(PlayerInputSequenceNumber left, PlayerInputSequenceNumber right) => new PlayerInputSequenceNumber(left._value + right._value);

        public static PlayerInputSequenceNumber operator -(PlayerInputSequenceNumber left, PlayerInputSequenceNumber right) => new PlayerInputSequenceNumber(left._value - right._value);

        public static PlayerInputSequenceNumber operator +(PlayerInputSequenceNumber left, uint right) => new PlayerInputSequenceNumber(left._value + right);

        public static PlayerInputSequenceNumber operator -(PlayerInputSequenceNumber left, uint right) => new PlayerInputSequenceNumber(left._value - right);

        public static PlayerInputSequenceNumber operator +(uint left, PlayerInputSequenceNumber right) => new PlayerInputSequenceNumber(left + right._value);

        public static PlayerInputSequenceNumber operator -(uint left, PlayerInputSequenceNumber right) => new PlayerInputSequenceNumber(left - right._value);

        public static PlayerInputSequenceNumber operator ++(PlayerInputSequenceNumber value) => new PlayerInputSequenceNumber(value._value + 1);
        public static PlayerInputSequenceNumber operator --(PlayerInputSequenceNumber value) => new PlayerInputSequenceNumber(value._value - 1);
    }
}
