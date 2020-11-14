namespace MultiplayerExample.Network.NetworkMessages.Server
{
    struct ClientJoinGameResponseMessage : INetworkMessage
    {
        public bool CanJoinGame;
        public string ErrorMessage;
        /// <summary>
        /// The scene to load which the player will start in.
        /// </summary>
        public SerializableGuid InGameSceneAssetId;

        public bool TryRead(NetworkMessageReader message)
        {
            bool isOk = true
                && message.Read(out CanJoinGame);
            if (CanJoinGame)
            {
                isOk = isOk
                    && message.Read(out InGameSceneAssetId);
            }
            else
            {
                isOk = isOk
                    && message.Read(out ErrorMessage);
            }

            return isOk;
        }

        public void WriteTo(NetworkMessageWriter message)
        {
            message.Write(ServerMessageType.ClientJoinGameResponse);

            message.Write(CanJoinGame);
            if (CanJoinGame)
            {
                message.Write(InGameSceneAssetId);
            }
            else
            {
                message.Write(ErrorMessage);
            }
        }
    }
}
