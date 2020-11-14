using MultiplayerExample.Core;
using Stride.Core.Mathematics;

namespace MultiplayerExample.Network.NetworkMessages.Client
{
    struct PlayerUpdateMessage : INetworkMessage
    {
        public SimulationTickNumber AcknowledgedServerSimulationTickNumber;

        public bool TryRead(NetworkMessageReader message)
        {
            bool isOk = true
                && message.Read(out AcknowledgedServerSimulationTickNumber);
            return isOk;
        }

        public void WriteTo(NetworkMessageWriter message)
        {
            message.Write(ClientMessageType.PlayerUpdate);
            message.Write(AcknowledgedServerSimulationTickNumber);
        }
    }

    struct PlayerUpdateInputMessage : INetworkMessageArray
    {
        public PlayerInputSequenceNumber PlayerInputSequenceNumber;
        public Vector2 MoveInput;
        public bool JumpRequestedInput;

        public bool TryReadHeader(NetworkMessageReader message, out ushort arraySize)
        {
            //arraySize = 0;

            bool isOk = true
                && message.Read(out arraySize);
            return isOk;
        }

        public bool TryReadNextArrayItem(NetworkMessageReader message)
        {
            bool isOk = true
                && message.Read(out PlayerInputSequenceNumber)
                && message.Read(out MoveInput)
                && message.Read(out JumpRequestedInput);

            return isOk;
        }

        public void WriteHeader(NetworkMessageWriter message, ushort arraySize)
        {
            message.Write(arraySize);
        }

        public void WriteNextArrayItem(NetworkMessageWriter message)
        {
            message.Write(PlayerInputSequenceNumber);
            message.Write(MoveInput);
            message.Write(JumpRequestedInput);
        }
    }
}
