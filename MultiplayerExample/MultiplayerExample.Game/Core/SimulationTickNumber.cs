using System;
using System.Diagnostics;

namespace MultiplayerExample.Core
{
    [DebuggerDisplay("{DebugDisplayString,nq}")]
    public readonly struct SimulationTickNumber : IEquatable<SimulationTickNumber>, IComparable<SimulationTickNumber>
    {
        private readonly long _value;

        internal string DebugDisplayString => _value.ToString();

        public SimulationTickNumber(long value)
        {
            _value = value;
        }

        public override string ToString() => _value.ToString();

        public static bool operator ==(SimulationTickNumber left, SimulationTickNumber right) => left.Equals(right);

        public static bool operator !=(SimulationTickNumber left, SimulationTickNumber right) => !left.Equals(right);

        public override bool Equals(object obj) => (obj is SimulationTickNumber val && Equals(val));

        public bool Equals(SimulationTickNumber other) => _value.Equals(other._value);

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(SimulationTickNumber other) => _value.CompareTo(other._value);

        public static implicit operator long(SimulationTickNumber serializablelong) => serializablelong._value;
        public static explicit operator SimulationTickNumber(long value) => new SimulationTickNumber(value);

        public static SimulationTickNumber operator +(SimulationTickNumber value) => value;
        public static SimulationTickNumber operator -(SimulationTickNumber value) => new SimulationTickNumber(-value._value);

        public static SimulationTickNumber operator +(SimulationTickNumber left, SimulationTickNumber right) => new SimulationTickNumber(left._value + right._value);

        public static SimulationTickNumber operator -(SimulationTickNumber left, SimulationTickNumber right) => new SimulationTickNumber(left._value - right._value);

        public static SimulationTickNumber operator +(SimulationTickNumber left, int right) => new SimulationTickNumber(left._value + right);

        public static SimulationTickNumber operator -(SimulationTickNumber left, int right) => new SimulationTickNumber(left._value - right);

        public static SimulationTickNumber operator +(int left, SimulationTickNumber right) => new SimulationTickNumber(left + right._value);

        public static SimulationTickNumber operator -(int left, SimulationTickNumber right) => new SimulationTickNumber(left - right._value);

        public static SimulationTickNumber operator ++(SimulationTickNumber value) => new SimulationTickNumber(value._value + 1);
        public static SimulationTickNumber operator --(SimulationTickNumber value) => new SimulationTickNumber(value._value - 1);

        public static bool operator <(SimulationTickNumber left, SimulationTickNumber right) => left._value < right._value;
        public static bool operator <=(SimulationTickNumber left, SimulationTickNumber right) => left._value <= right._value;
        public static bool operator >(SimulationTickNumber left, SimulationTickNumber right) => left._value > right._value;
        public static bool operator >=(SimulationTickNumber left, SimulationTickNumber right) => left._value >= right._value;
    }
}
