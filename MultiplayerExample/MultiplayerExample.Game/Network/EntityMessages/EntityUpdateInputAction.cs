using MultiplayerExample.Core;
using MultiplayerExample.Network.NetworkMessages;
using MultiplayerExample.Network.SnapshotStores;

namespace MultiplayerExample.Network.EntityMessages
{
    struct EntityUpdateInputAction : INetworkMessageArray
    {
        /// <summary>
        /// The tick number this input was applied at.
        /// </summary>
        public SimulationTickNumber SimulationTickNumber;
        public SerializableGuid NetworkEntityId;
        public InputActionType InputActionType;

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
                && message.Read(out SimulationTickNumber)
                && message.Read(out NetworkEntityId)
                && message.ReadEnumFromByte(out InputActionType);
            return isOk;
        }

        public void WriteHeader(NetworkMessageWriter message, ushort arraySize)
        {
            //message.Write((byte)EntityUpdateType.InputAction);
            message.Write(arraySize);
        }

        public void WriteNextArrayItem(NetworkMessageWriter message)
        {
            message.Write(SimulationTickNumber);
            message.Write(NetworkEntityId);
            message.Write((byte)InputActionType);
        }
    }
}
