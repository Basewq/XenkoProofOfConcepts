using LiteNetLib.Utils;
using MultiplayerExample.Core;
using MultiplayerExample.Network.NetworkMessages.Client;
using MultiplayerExample.Network.NetworkMessages.Server;
using Stride.Core.Mathematics;
using System;

namespace MultiplayerExample.Network.NetworkMessages
{
    public readonly struct NetworkMessageWriter
    {
        private readonly NetDataWriter _writer;

        public NetworkMessageWriter(NetDataWriter writer)
        {
            _writer = writer;
        }

        public static implicit operator NetworkMessageWriter(NetDataWriter writer) => new NetworkMessageWriter(writer);
        public static implicit operator NetDataWriter(NetworkMessageWriter writer) => writer._writer;

        /// <summary>
        /// Number of bytes currently written.
        /// </summary>
        public int Length => _writer.Length;

        /// <summary>
        /// Reset the writer back to the start of the stream.
        /// </summary>
        public void Reset()
        {
            _writer.Reset();
        }

        public void Write(bool value) => _writer.Put(value);

        public void Write(byte value) => _writer.Put(value);

        public void Write(int value) => _writer.Put(value);

        public void Write(long value) => _writer.Put(value);

        public void Write(ushort value) => _writer.Put(value);

        public void Write(uint value) => _writer.Put(value);

        public void Write(ulong value) => _writer.Put(value);

        public void Write(float value) => _writer.Put(value);

        public void Write(double value) => _writer.Put(value);

        public void Write(string value) => _writer.Put(value);

        public void Write(in SerializableGuid guid)
        {
            _writer.Put(guid.LowerBytes);
            _writer.Put(guid.UpperBytes);
        }

        public void Write(in Vector2 vector)
        {
            _writer.Put(vector.X);
            _writer.Put(vector.Y);
        }

        public void Write(in Vector3 vector)
        {
            _writer.Put(vector.X);
            _writer.Put(vector.Y);
            _writer.Put(vector.Z);
        }

        public void Write(in Quaternion quaternion)
        {
            _writer.Put(quaternion.X);
            _writer.Put(quaternion.Y);
            _writer.Put(quaternion.Z);
            _writer.Put(quaternion.W);
        }

        public void Write(in TimeSpan timeSpan)
        {
            _writer.Put(timeSpan.Ticks);
        }

        public void Write(in SimulationTickNumber simTickNumber)
        {
            _writer.Put((long)simTickNumber);
        }

        public void Write(in PlayerInputSequenceNumber playerInputSeqNumber)
        {
            _writer.Put((uint)playerInputSeqNumber);
        }

        internal void Write(ClientMessageType messageType)
        {
            _writer.Put((byte)messageType);
        }

        internal void Write(ServerMessageType messageType)
        {
            _writer.Put((byte)messageType);
        }
    }
}
