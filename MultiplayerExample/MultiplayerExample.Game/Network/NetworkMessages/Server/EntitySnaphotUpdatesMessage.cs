using MultiplayerExample.Core;

namespace MultiplayerExample.Network.NetworkMessages.Server
{
    struct EntitySnaphotUpdatesMessage : INetworkMessage
    {
        /// <summary>
        /// Server confirmation of the last input number has received from the player.
        /// </summary>
        public PlayerInputSequenceNumber AcknowledgedLastReceivedPlayerInputSequenceNumber;
        /// <summary>
        /// Server's last applied input received from the player. This may differ from <see cref="AcknowledgedLastReceivedPlayerInputSequenceNumber"/> since
        /// only one input can be applied per simulation tick, so the remaining input is queued up for future ticks.
        /// </summary>
        public PlayerInputSequenceNumber LastAppliedServerPlayerInputSequenceNumber;

        /// <summary>
        /// The server's simulation tick number at the time of sending the message.
        /// </summary>
        public SimulationTickNumber ServerSimulationTickNumber;

        public bool TryRead(NetworkMessageReader message)
        {
            bool isOk = true
                && message.Read(out AcknowledgedLastReceivedPlayerInputSequenceNumber)
                && message.Read(out LastAppliedServerPlayerInputSequenceNumber)
                && message.Read(out ServerSimulationTickNumber);

            return isOk;
        }

        public void WriteTo(NetworkMessageWriter message)
        {
            message.Write(ServerMessageType.SnaphotUpdates);

            message.Write(AcknowledgedLastReceivedPlayerInputSequenceNumber);
            message.Write(LastAppliedServerPlayerInputSequenceNumber);
            message.Write(ServerSimulationTickNumber);
        }
    }
}
