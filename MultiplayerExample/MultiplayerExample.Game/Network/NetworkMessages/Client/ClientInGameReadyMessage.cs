namespace MultiplayerExample.Network.NetworkMessages.Client
{
    struct ClientInGameReadyMessage : INetworkMessage
    {
        public bool TryRead(NetworkMessageReader message)
        {
            bool isOk = true;
            return isOk;
        }

        public void WriteTo(NetworkMessageWriter message)
        {
            message.Write(ClientMessageType.ClientInGameReady);
        }
    }
}
