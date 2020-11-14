using Stride.Core;
using Stride.Engine;

namespace MultiplayerExample.Network
{
    [DataContract]
    public class NetworkPlayerComponent : EntityComponent
    {
        internal string PlayerName { get; set; }
    }
}
