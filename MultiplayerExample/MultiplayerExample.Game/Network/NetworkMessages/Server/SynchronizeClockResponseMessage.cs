namespace MultiplayerExample.Network.NetworkMessages.Server
{
    struct SynchronizeClockResponseMessage : INetworkMessage
    {
        /// <summary>
        /// The timestamp that the client sent in its <see cref="Client.SynchronizeClockRequestMessage.ClientOSTimestamp"/>.
        /// </summary>
        public long ClientOSTimeStamp;
        /// <summary>
        /// The server's world time at the time of receiving the <see cref="Client.SynchronizeClockRequestMessage"/>.
        /// </summary>
        public long ServerWorldTimeInTicks;

        public bool TryRead(NetworkMessageReader message)
        {
            bool isOk = true
                && message.Read(out ClientOSTimeStamp)
                && message.Read(out ServerWorldTimeInTicks);
            return isOk;
        }

        public void WriteTo(NetworkMessageWriter message)
        {
            message.Write(ServerMessageType.SynchronizeClockResponse);
            message.Write(ClientOSTimeStamp);
            message.Write(ServerWorldTimeInTicks);
        }
    }
}
