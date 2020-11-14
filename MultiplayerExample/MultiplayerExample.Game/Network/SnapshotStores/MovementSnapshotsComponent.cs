using MultiplayerExample.Core;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace MultiplayerExample.Network.SnapshotStores
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(MovementSnapshotsProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [DefaultEntityComponentProcessor(typeof(MovementSnapshotsInputProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [DefaultEntityComponentProcessor(typeof(MovementSnapshotsRenderProcessor), ExecutionMode = ExecutionMode.Runtime)]
    public class MovementSnapshotsComponent : EntityComponent
    {
        internal SnapshotStore<MovementData> SnapshotStore;

        [Display("Run Speed")]
        public float MaxRunSpeed { get; set; } = 8.5f;

        /// <summary>
        /// Allow for some latency from the user input to make jumping appear more natural
        /// </summary>
        [Display("Jump Time Limit")]
        public float JumpReactionThreshold { get; set; } = 0.15f;

        internal struct MovementData : ISnapshotData
        {
            public SimulationTickNumber SimulationTickNumber { get; set; }

            internal SnapshotType SnapshotType;

            /// <summary>
            /// Allow some inertia to the movement.
            /// </summary>
            internal Vector3 MoveDirection;
            /// <summary>
            /// Used to determine the walk/run animation state. Value in range [0...1]
            /// </summary>
            internal float MoveSpeedDecimalPercentage;
            /// <summary>
            /// Rotation in degrees.
            /// </summary>
            internal float YawOrientation;
            /// <summary>
            /// When the character falls off a surface, allow for some reaction time.
            /// </summary>
            internal float JumpReactionRemaining;

            /// <summary>
            /// The movement velocity applied that resulted in <see cref="LocalPosition"/>.
            /// </summary>
            internal Vector3 CurrentMoveInputVelocity;
            internal Vector3 LocalPosition;
            internal Quaternion LocalRotation;
            internal bool IsGrounded;
            /// <summary>
            /// The resulting linear velocity after the physics step.
            /// </summary>
            internal Vector3 PhysicsEngineLinearVelocity;
            //internal Vector3 PhysicsEngineAngularVelocity;     // Unused

            /// <summary>
            /// Used in ClientPredictionSnapshotsComponent.PredictedMovements, and applied inputs from the server.
            /// On the client, this may be zero if input has been lost.
            /// </summary>
            internal PlayerInputSequenceNumber PlayerInputSequenceNumberApplied;
        }
    }
}
