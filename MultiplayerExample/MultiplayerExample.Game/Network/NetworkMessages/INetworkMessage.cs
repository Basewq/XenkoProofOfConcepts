namespace MultiplayerExample.Network.NetworkMessages
{
    internal interface INetworkMessage
    {
        bool TryRead(NetworkMessageReader message);

        void WriteTo(NetworkMessageWriter message);
    }

    internal interface INetworkMessageArray
    {
        bool TryReadHeader(NetworkMessageReader message, out ushort arraySize);
        bool TryReadNextArrayItem(NetworkMessageReader message);

        void WriteHeader(NetworkMessageWriter message, ushort arraySize);
        void WriteNextArrayItem(NetworkMessageWriter message);
    }
}
