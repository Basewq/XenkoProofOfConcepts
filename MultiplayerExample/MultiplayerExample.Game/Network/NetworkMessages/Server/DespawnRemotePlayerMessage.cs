using MultiplayerExample.Core;

namespace MultiplayerExample.Network.NetworkMessages.Server
{
    struct DespawnRemotePlayerMessage : INetworkMessage
    {
        /// <summary>
        /// The PlayerId is the same as the entity's NetworkEntityId for their avatar.
        /// </summary>
        public SerializableGuid PlayerId;
        public SimulationTickNumber SimulationTickNumber;

        public bool TryRead(NetworkMessageReader message)
        {
            bool isOk = true
                && message.Read(out PlayerId)
                && message.Read(out SimulationTickNumber);

            return isOk;
        }

        public void WriteTo(NetworkMessageWriter message)
        {
            message.Write(ServerMessageType.DespawnRemotePlayer);

            message.Write(PlayerId);
            message.Write(SimulationTickNumber);
        }
    }
}
