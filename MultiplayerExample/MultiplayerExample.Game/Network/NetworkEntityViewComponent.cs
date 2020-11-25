using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace MultiplayerExample.Network
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(NetworkEntityViewProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [DefaultEntityComponentProcessor(typeof(NetworkMovementViewProcessor), ExecutionMode = ExecutionMode.Runtime)]
    public class NetworkEntityViewComponent : EntityComponent
    {
        /// <summary>
        /// The entity whose data is updated directly from the server.
        /// </summary>
        public Entity NetworkedEntity;
    }
}
