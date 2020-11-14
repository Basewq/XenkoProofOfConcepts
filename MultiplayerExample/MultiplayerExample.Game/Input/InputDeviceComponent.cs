using MultiplayerExample.Network.SnapshotStores;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Input;
using System.ComponentModel;

namespace MultiplayerExample.Input
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(InputDeviceRegistrationProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [DefaultEntityComponentProcessor(typeof(InputSnapshotsInGameInputProcessor), ExecutionMode = ExecutionMode.Runtime)]
    public class InputDeviceComponent : EntityComponent
    {
        [DataMember(10)]
        public int PlayerIndex { get; set; }

        /// <summary>
        /// Only applies to in-game.
        /// </summary>
        [DataMember(50)]
        public bool IsKeyboardControlsEnabled { get; set; }
        /// <summary>
        /// Only applies to in-game.
        /// </summary>
        [DataMember(60)]
        public bool IsMouseControlsEnabled { get; set; }

        [DataMember(70)]
        public bool IsControllerEnabled { get; set; }

        [DataMember(75)]
        [DefaultValue(0.25f)]
        public float DeadZoneThreshold { get; set; } = 0.25f;

        /// <summary>
        /// Multiplies move movement by this amount to apply aim rotations
        /// </summary>
        public float MouseSensitivity = 50.0f;

        // Devices
        [DataMemberIgnore]
        public IKeyboardDevice ActiveKeyboard { get; set; }
        [DataMemberIgnore]
        public IMouseDevice ActiveMouse { get; set; }

        [DataMemberIgnore]
        public IGamePadDevice ActiveController { get; set; }
    }
}
