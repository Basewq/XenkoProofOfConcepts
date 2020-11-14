namespace MultiplayerExample.Network
{
    public enum NetworkOwnerType : byte
    {
        /// <summary>
        /// Entity is owned and controlled by a player.
        /// </summary>
        Player,
        /// <summary>
        /// Entity is owned and controller by the server.
        /// </summary>
        Server
    }
}
