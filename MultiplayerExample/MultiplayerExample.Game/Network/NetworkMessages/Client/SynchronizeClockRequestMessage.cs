namespace MultiplayerExample.Network.NetworkMessages.Client
{
    struct SynchronizeClockRequestMessage : INetworkMessage
    {
        /// <summary>
        /// The client's OS timestamp at the time of sending this message.
        /// This is used to determine the latency between the client and server.
        /// </summary>
        public long ClientOSTimestamp;

        public bool TryRead(NetworkMessageReader message)
        {
            bool isOk = true
                && message.Read(out ClientOSTimestamp);
            return isOk;
        }

        public void WriteTo(NetworkMessageWriter message)
        {
            message.Write(ClientMessageType.SynchronizeClockRequest);
            message.Write(ClientOSTimestamp);
        }
    }
}
