using MultiplayerExample.Core;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using System;

namespace MultiplayerExample.Network.SnapshotStores
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(InputSnapshotsInitializerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    public class InputSnapshotsComponent : EntityComponent
    {
        public CameraComponent Camera { get; set; }

        internal SnapshotStore<InputCommandSet> SnapshotStore;

        internal PlayerInputSequenceNumber ServerLastAcknowledgedPlayerInputSequenceNumber;
        internal PlayerInputSequenceNumber ServerLastAppliedPlayerInputSequenceNumber;
        /// <summary>
        /// Note we assume this will always be sorted by PlayerInputSequenceNumber in ascending order.
        /// Contains the list of inputs that have been applied on the local client, and either not acknowledged or
        /// not yet applied on the server.
        /// </summary>
        internal FastList<InputCommandSet> PendingInputs;

        internal PlayerInputSequenceNumber NextPlayerInputSequenceNumber = new PlayerInputSequenceNumber(1);

        internal PlayerInputSequenceNumber GetNextPlayerInputSequenceNumber()
        {
            var returnNextPlayerInputSequenceNumber = NextPlayerInputSequenceNumber;
            NextPlayerInputSequenceNumber++;
            return returnNextPlayerInputSequenceNumber;
        }

        internal struct InputCommandSet : ISnapshotData
        {
            public SimulationTickNumber SimulationTickNumber { get; set; }

            public PlayerInputSequenceNumber PlayerInputSequenceNumber;

            public Vector2 MoveInput;
            public bool IsJumpButtonDown;

            // Server specific fields
            public InputActionType ConfirmedInputActionType;

            // Client-side only fields, these are not sent to the server
            public Vector2 CameraMoveInput;
            public bool IsCameraLockInButtonDown;
            public bool IsCameraUnlockButtonDown;

            //public InputActionSlot ActionSlot0;
            //public InputActionSlot ActionSlot1;
            //public InputActionSlot ActionSlot2;
            //public InputActionSlot ActionSlot3;
        }

        //internal struct InputActionSlot
        //{
        //    internal InputActionType InputActionType;
        //    internal InputValue InputValue;
        //}
        //
        //[StructLayout(LayoutKind.Explicit)]
        //internal struct InputValue
        //{
        //    [FieldOffset(0)]
        //    public bool IsDown;
        //
        //    [FieldOffset(0)]
        //    public float FloatValue;
        //
        //    [FieldOffset(0)]
        //    public Vector2 Vector2Value;
        //}
    }

    enum InputActionType : byte
    {
        NoAction,
        Jump,
        Melee,
        Shoot,
    }
}
