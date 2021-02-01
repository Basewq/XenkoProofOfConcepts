using System;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace EntityProcessorExample.Player
{
    /* Notes:
     * Make sure the class is public, to ensure Xenko Editor detects it.
     *
     * Ensure DataContract is Stride.Core.DataContract, not System.Runtime.Serialization.DataContract, otherwise
     * Xenko Editor ignores this component.
     *
     * DefaultEntityComponentProcessor attribute ensures PlayerInputProcessor is automatically registered
     * to the EntityManager/SceneInstance.
     */
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(PlayerInputProcessor))]
    public class PlayerInputComponent : EntityComponent
    {
        public bool IsKeyboardEnabled { get; set; }

        [DataMemberIgnore]  // Does not expose this property to Xenko Editor.
        internal Guid? ActiveKeyboardId { get; set; }
    }
}
