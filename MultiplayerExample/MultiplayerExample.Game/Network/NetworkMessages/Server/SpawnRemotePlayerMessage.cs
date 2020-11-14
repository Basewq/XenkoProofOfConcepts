using MultiplayerExample.Core;
using Stride.Core.Mathematics;

namespace MultiplayerExample.Network.NetworkMessages.Server
{
    struct SpawnRemotePlayerMessage : INetworkMessage
    {
        /// <summary>
        /// The PlayerId is the same as the entity's NetworkEntityId for their avatar.
        /// </summary>
        public SerializableGuid PlayerId;
        public SimulationTickNumber SimulationTickNumber;
        public string PlayerName;
        public Vector3 Position;
        public Quaternion Rotation;

        public bool TryRead(NetworkMessageReader message)
        {
            bool isOk = true
                && message.Read(out PlayerId)
                && message.Read(out SimulationTickNumber)
                && message.Read(out PlayerName)
                && message.Read(out Position)
                && message.Read(out Rotation);

            return isOk;
        }

        public void WriteTo(NetworkMessageWriter message)
        {
            message.Write(ServerMessageType.SpawnRemotePlayer);

            message.Write(PlayerId);
            message.Write(SimulationTickNumber);
            message.Write(PlayerName);
            message.Write(Position);
            message.Write(Rotation);
        }
    }
}
