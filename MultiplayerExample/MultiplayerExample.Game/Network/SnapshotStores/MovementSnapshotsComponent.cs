using MultiplayerExample.Core;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using System;

namespace MultiplayerExample.Network.SnapshotStores
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(MovementSnapshotsProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [DefaultEntityComponentProcessor(typeof(MovementSnapshotsInputProcessor), ExecutionMode = ExecutionMode.Runtime)]
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

        internal static void InitializeNewMovementData(
            ref MovementData movementData, Vector3 position, float jumpReactionThreshold)
        {
            movementData.MoveDirection = Vector3.Zero;
            var faceDir = Vector3.UnitZ;
            movementData.SetRotationFromFacingDirection(faceDir);
            movementData.JumpReactionRemaining = jumpReactionThreshold;
            movementData.LocalPosition = position;
            movementData.PhysicsEngineLinearVelocity = Vector3.Zero;
        }

        internal struct MovementData : ISnapshotData
        {
            public SimulationTickNumber SimulationTickNumber { get; set; }

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
            /// <summary>
            /// The resulting position after the physics step.
            /// </summary>
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

            internal void SetRotationFromFacingDirection(Vector3 facingDirection)
            {
                YawOrientation = MathUtil.RadiansToDegrees((float)Math.Atan2(-facingDirection.Z, facingDirection.X) + MathUtil.PiOverTwo);
                LocalRotation = Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(YawOrientation), 0, 0);
            }

            internal void SetRotationFromYawOrientation(float yawOrientation)
            {
                YawOrientation = yawOrientation;
                LocalRotation = Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(yawOrientation), 0, 0);
            }

            internal void SetRotationFromQuaternion(Quaternion rotation)
            {
                LocalRotation = rotation;
                Quaternion.RotationYawPitchRoll(ref rotation, out YawOrientation, out _, out _);
            }
        }
    }
}
