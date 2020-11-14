using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MultiplayerExample.Network
{
    [DebuggerDisplay("{DebugDisplayString,nq}")]
    [StructLayout(LayoutKind.Explicit)]
    public struct SerializableGuid : IEquatable<SerializableGuid>, IComparable<SerializableGuid>
    {
        // HACK: we know the underlying data of the Guid is just two long fields.
        // LowerBytes & UpperBytes overlap with the Guid field so we can access its internal fields
        // in a sneaky way. A Guid is 128 bits so we need 128 bits worth of fields.
        [FieldOffset(0)]
        public long LowerBytes;
        [FieldOffset(sizeof(long))]
        public long UpperBytes;

        [FieldOffset(0)]
        public Guid Guid;

        internal string DebugDisplayString => Guid.ToString();

        public override string ToString() => Guid.ToString();

        public static bool operator ==(in SerializableGuid left, in SerializableGuid right) => left.Equals(right);

        public static bool operator !=(in SerializableGuid left, in SerializableGuid right) => !left.Equals(right);

        public override bool Equals(object obj) => (obj is SerializableGuid val && Equals(val));

        public bool Equals(SerializableGuid other) => Guid.Equals(other.Guid);

        public override int GetHashCode() => Guid.GetHashCode();

        public int CompareTo(SerializableGuid other) => Guid.CompareTo(other.Guid);

        public static implicit operator Guid(in SerializableGuid serializableGuid) => serializableGuid.Guid;
        public static implicit operator SerializableGuid(in Guid guid) => new SerializableGuid { Guid = guid };
    }
}
