namespace MultiplayerExample.Network.NetworkMessages.Client
{
    struct ClientJoinGameRequestMessage : INetworkMessage
    {
        public string PlayerName;

        public ClientJoinGameRequestMessage(string playerName)
        {
            PlayerName = playerName;
        }

        public bool TryRead(NetworkMessageReader message)
        {
            bool isOk = true
                && message.Read(out PlayerName);
            return isOk;
        }

        public void WriteTo(NetworkMessageWriter message)
        {
            message.Write(ClientMessageType.ClientJoinGame);
            message.Write(PlayerName);
        }
    }
}
