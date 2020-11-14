using LiteNetLib;
using MultiplayerExample.Core;
using MultiplayerExample.Utilities;
using Stride.Core.Mathematics;
using System;

namespace MultiplayerExample.Network.NetworkMessages
{
    internal readonly struct NetworkMessageReader
    {
        private readonly NetPacketReader _reader;

        public NetworkMessageReader(NetPacketReader reader)
        {
            _reader = reader;
        }

        public static implicit operator NetworkMessageReader(NetPacketReader reader) => new NetworkMessageReader(reader);
        public static implicit operator NetPacketReader(NetworkMessageReader reader) => reader._reader;

        public bool Read(out bool value) => _reader.TryGetBool(out value);

        public bool Read(out byte value) => _reader.TryGetByte(out value);

        public bool Read(out int value) => _reader.TryGetInt(out value);

        public bool Read(out long value) => _reader.TryGetLong(out value);

        public bool Read(out ushort value) => _reader.TryGetUShort(out value);

        public bool Read(out uint value) => _reader.TryGetUInt(out value);

        public bool Read(out ulong value) => _reader.TryGetULong(out value);

        public bool Read(out float value) => _reader.TryGetFloat(out value);

        public bool Read(out double value) => _reader.TryGetDouble(out value);

        internal bool Read(out string value) => _reader.TryGetString(out value);

        public bool ReadEnumFromByte<T>(out T value) where T : struct, Enum
        {
            if (_reader.TryGetByte(out var innerValue))
            {
                value = EnumByteExt<T>.ToEnumConverter(innerValue);
                return true;
            }
            value = default;
            return false;
        }

        public bool Read(out SerializableGuid value)
        {
            value = default;
            return _reader.TryGetLong(out value.LowerBytes)
                && _reader.TryGetLong(out value.UpperBytes);
        }

        public bool Read(out Vector2 value)
        {
            value = default;
            return _reader.TryGetFloat(out value.X)
                && _reader.TryGetFloat(out value.Y);
        }

        public bool Read(out Vector3 value)
        {
            value = default;
            return _reader.TryGetFloat(out value.X)
                && _reader.TryGetFloat(out value.Y)
                && _reader.TryGetFloat(out value.Z);
        }

        public bool Read(out Quaternion value)
        {
            value = default;
            return _reader.TryGetFloat(out value.X)
                && _reader.TryGetFloat(out value.Y)
                && _reader.TryGetFloat(out value.Z)
                && _reader.TryGetFloat(out value.W);
        }

        public bool Read(out TimeSpan value)
        {
            if (_reader.TryGetLong(out var innerValue))
            {
                value = TimeSpan.FromTicks(innerValue);
                return true;
            }

            value = default;
            return false;
        }

        public bool Read(out SimulationTickNumber value)
        {
            if (_reader.TryGetLong(out var innerValue))
            {
                value = new SimulationTickNumber(innerValue);
                return true;
            }

            value = default;
            return false;
        }

        public bool Read(out PlayerInputSequenceNumber value)
        {
            if (_reader.TryGetUInt(out var innerValue))
            {
                value = new PlayerInputSequenceNumber(innerValue);
                return true;
            }
            value = default;
            return false;
        }
    }
}
