using MultiplayerExample.Core;
using MultiplayerExample.Engine;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Physics;
using System;
using System.Diagnostics;

namespace MultiplayerExample.Network.SnapshotStores
{
    class MovementSnapshotsInputProcessor : EntityProcessor<MovementSnapshotsComponent, MovementSnapshotsInputProcessor.AssociatedData>,
        IInGameProcessor, IPreUpdateProcessor
    {
        private SceneSystem _sceneSystem;
        private Simulation _simulation;
        private GameClockManager _gameClockManager;
        private GameEngineContext _gameEngineContext;
        private PhysicsProcessor _physicsProcessor;

        public bool IsEnabled { get; set; }

        public MovementSnapshotsInputProcessor() : base(typeof(TransformComponent), typeof(CharacterComponent), typeof(InputSnapshotsComponent))
        {
            Order = 10;         // Ensure this occurs after InGamePlayerInputProcessor
            // Not using Enabled property, because that completely disables the processor, where it doesn't even pick up newly added entities
            IsEnabled = true;
        }

        protected override void OnSystemAdd()
        {
            _sceneSystem = Services.GetSafeServiceAs<SceneSystem>();

            _gameClockManager = Services.GetSafeServiceAs<GameClockManager>();
            _gameEngineContext = Services.GetService<GameEngineContext>();
        }

        protected override AssociatedData GenerateComponentData([NotNull] Entity entity, [NotNull] MovementSnapshotsComponent component)
        {
            return new AssociatedData
            {
                // Can also add other info/components here
                TransformComponent = entity.Transform,
                CharacterComponent = entity.Get<CharacterComponent>(),
                InputSnapshotsComponent = entity.Get<InputSnapshotsComponent>(),

                ClientPredictionSnapshotsComponent = entity.Get<ClientPredictionSnapshotsComponent>(),

                ModelChildEntity = entity.GetChild(0),
            };
        }

        protected override bool IsAssociatedDataValid([NotNull] Entity entity, [NotNull] MovementSnapshotsComponent component, [NotNull] AssociatedData associatedData)
        {
            return associatedData.TransformComponent == entity.Transform
                && associatedData.CharacterComponent == entity.Get<CharacterComponent>()
                && associatedData.InputSnapshotsComponent == entity.Get<InputSnapshotsComponent>();
        }

        public void PreUpdate(GameTime gameTime)
        {
            _physicsProcessor ??= _sceneSystem.SceneInstance.GetProcessor<PhysicsProcessor>();
            _simulation ??= _physicsProcessor?.Simulation;
            if (_simulation == null)
            {
                // Still not loaded yet?
                return;
            }
            if (!_gameClockManager.SimulationClock.IsNextSimulation)
            {
                // Only update during a simulation update
                return;
            }

            var simDeltaTime = _simulation.FixedTimeStep;
            var simTickNumber = _gameClockManager.SimulationClock.SimulationTickNumber;
            foreach (var kv in ComponentDatas)
            {
                var movementSnapshotsComp = kv.Key;
                var data = kv.Value;
                var inputSnapshotsComp = data.InputSnapshotsComponent;
                var clientPredictionSnapshotsComp = data.ClientPredictionSnapshotsComponent;
                var characterComp = data.CharacterComponent;
                if (_gameEngineContext.IsClient && clientPredictionSnapshotsComp != null)
                {
                    Debug.Assert(characterComp != null, $"Client predicted entity requires {nameof(CharacterComponent)}.");
                    // Add new predicted position
                    var inputFindResult = inputSnapshotsComp.SnapshotStore.TryFindSnapshot(simTickNumber);
                    ref var curInputData = ref inputFindResult.Result;
                    if (!inputFindResult.IsFound)
                    {
                        continue;
                    }

                    var predictedMovements = clientPredictionSnapshotsComp.PredictedMovements;
                    ref var predictedMovementData = ref ClientPredictionSnapshotsInitializerProcessor.CreateNewSnapshotData(
                        predictedMovements, simTickNumber, movementSnapshotsComp, data.TransformComponent, data.ModelChildEntity.Transform);
                    SetPredictedMovementData(simDeltaTime, movementSnapshotsComp, characterComp,
                        ref predictedMovementData, ref curInputData);
                }
                else if (_gameEngineContext.IsServer)
                {
                    // Update positions
                    var movementFindResult = movementSnapshotsComp.SnapshotStore.TryFindSnapshot(simTickNumber);
                    var inputFindResult = inputSnapshotsComp.SnapshotStore.TryFindSnapshot(simTickNumber);
                    if (!movementFindResult.IsFound || !inputFindResult.IsFound)
                    {
                        continue;
                    }

                    ref var curMovementData = ref movementFindResult.Result;
                    ref var curInputData = ref inputFindResult.Result;
                    Move(movementSnapshotsComp, data.CharacterComponent, ref curMovementData, ref curInputData);
                    TryJump(simDeltaTime, movementSnapshotsComp, data.CharacterComponent, ref curMovementData, ref curInputData);
#if DEBUG
                    //if (curInputData.MoveInput.LengthSquared() > 0)
                    //{
                    //    Debug.WriteLine($"MovementSnapshotsInputProcessor.Update: {curInputData.MoveInput} - Sim {simTickNumber}");
                    //}
#endif
                }
            }
        }

#if DEBUG
        int debugPrint = 0;
        int triggerDebugPrint1 = 2;
        int triggerDebugPrint2 = 3;
#endif
        internal void Resimulate(SimulationTickNumber currentSimulationTickNumber, FastList<PredictMovementEntityData> resimulateEntities)
        {
            _physicsProcessor ??= _sceneSystem.SceneInstance.GetProcessor<PhysicsProcessor>();
            _simulation ??= _physicsProcessor?.Simulation;
            Debug.Assert(_physicsProcessor != null, "Physics Processor needs to be created.");
            Debug.Assert(_simulation != null, "Physics Simulation needs to be created.");
            Debug.Assert(_gameEngineContext.IsClient, "Server should not be resimulating.");

            int maxResimInputCount = 0;
            // Reset the initial positions to the start of the simulation time
            for (int i = 0; i < resimulateEntities.Count; i++)
            {
                ref var entityData = ref resimulateEntities.Items[i];

                var transformComp = entityData.TransformComponent;
                var characterComp = entityData.CharacterComponent;
                var movementSnapshotsComp = entityData.MovementSnapshotsComponent;

                Debug.Assert(!movementSnapshotsComp.SnapshotStore.IsEmpty);

                ref var movementData = ref movementSnapshotsComp.SnapshotStore.GetLatest();
                transformComp.Position = movementData.LocalPosition;
                //modelChildTransform.Rotation  movementData.LocalRotation;
                BulletPhysicsExt.SetLinearVelocity(characterComp, ref movementData.PhysicsEngineLinearVelocity);
                transformComp.UpdateWorldMatrix();
                characterComp.UpdatePhysicsTransformation();        // Update in the physics engine
                BulletPhysicsExt.SimulateCharacter(characterComp, _simulation, deltaTimeInSeconds: 0);      // We need to 'resimulate' with zero delta time to ensure character IsGround or !IsGround is reset properly

                // Find the max number of resimulations we'll need to run
                var inputSnapshotsComp = entityData.InputSnapshotsComponent;
                maxResimInputCount = Math.Max(maxResimInputCount, inputSnapshotsComp.PendingInputs.Count);

#if DEBUG
                var pm = entityData.ClientPredictionSnapshotsComponent.PredictedMovements;
                //if (pm.Count > 0 && pm[pm.Count - 1].LocalPosition.X >= 0)
                if (transformComp.Position.X != -5)
                {
                    //debugPrint++;
                    //for (int jjj = movementSnapshotsComp.SnapshotStore.Count - 1; jjj >= 0; jjj--)
                    //{
                    //    ref var move0 = ref movementSnapshotsComp.SnapshotStore.GetPrevious(jjj);
                    //    Debug.WriteLine($"Resim SvrPos {move0.LocalPosition} - Sim {move0.SimulationTickNumber} - PIDApplied {move0.PlayerInputSequenceNumberApplied}");
                    //}
                    //Debug.WriteLine($"Resim PrevInputs - Start Sim {currentSimulationTickNumber} - PIDLastAckd {entityData.InputSnapshotsComponent.ServerLastAcknowledgedPlayerInputSequenceNumber} - PIDLastAppd {entityData.InputSnapshotsComponent.ServerLastAppliedPlayerInputSequenceNumber}");
                    //foreach (var m in pm)
                    //{
                    //    Debug.WriteLine($"Resim OldPos {m.LocalPosition} - Sim {m.SimulationTickNumber} - PIDApplied {m.PlayerInputSequenceNumberApplied} - MvDir {m.MoveDirection}");
                    //}
                }

                //Debug.WriteLine($"ResimPrep --- Show prev predictions:");
                //foreach (var m in pm)
                //{
                //    Debug.WriteLine($"Resim Pos {m.LocalPosition} - PIDApplied {m.PlayerInputSequenceNumberApplied} - MvDir {m.MoveDirection} - Vel {m.PhysicsEngineLinearVelocity} - IsGnd {m.IsGrounded}");
                //}
                //for (int jjjj = movementSnapshotsComp.SnapshotStore.Count - 1; jjjj >= 0; jjjj--)
                //{
                //    ref var m = ref movementSnapshotsComp.SnapshotStore.GetPrevious(jjjj);
                //    Debug.WriteLine($"Resim NewPos {m.LocalPosition} - PIDApplied {m.PlayerInputSequenceNumberApplied} - MvDir {m.MoveDirection} - Vel {m.PhysicsEngineLinearVelocity}");
                //}
#endif

                // Remove old predictions
                var clientPredictionSnapshotsComp = entityData.ClientPredictionSnapshotsComponent;
                clientPredictionSnapshotsComp.PredictedMovements.Clear();
                // Add a fake prediction values, which is actually just the most recent two server values.
                // This is done so MovementSnapshotsRenderProcessor will always have two values to use for interpolation
                // even if all input predictions have been consumed.
                var predictedMovements = clientPredictionSnapshotsComp.PredictedMovements;
                var modelChildTransformComp = entityData.ModelChildEntity.Transform;
                int movementCopyCount = Math.Min(2, movementSnapshotsComp.SnapshotStore.Count);
                for (int mvmtIndex = movementCopyCount - 1; mvmtIndex >= 0; mvmtIndex--)
                {
                    ref var predictedMovementData = ref ClientPredictionSnapshotsInitializerProcessor.CreateNewSnapshotData(
                        predictedMovements, currentSimulationTickNumber - 1, movementSnapshotsComp, transformComp, modelChildTransformComp);
                    // Copy the data directly over
                    predictedMovementData = movementSnapshotsComp.SnapshotStore.GetPrevious(mvmtIndex);
                }
            }

#if DEBUG
            if (debugPrint == triggerDebugPrint1 || debugPrint == triggerDebugPrint2)
            {
                Debug.WriteLine($"Resim Start Sim {currentSimulationTickNumber}");
            }
#endif
            float simDeltaTime = _simulation.FixedTimeStep;
            for (int inputIdx = 0; inputIdx < maxResimInputCount; inputIdx++)
            {
                for (int i = 0; i < resimulateEntities.Count; i++)
                {
                    ref var entityData = ref resimulateEntities.Items[i];
                    var inputSnapshotsComp = entityData.InputSnapshotsComponent;
                    if (inputIdx >= inputSnapshotsComp.PendingInputs.Count)
                    {
                        // If multiple entities require resimulation, technically they should all have the same
                        // number of unacked inputs, but maybe this won't be true if further code optimization is done?
                        continue;
                    }

                    var transformComp = entityData.TransformComponent;
                    var modelChildTransformComp = entityData.ModelChildEntity.Transform;
                    var characterComp = entityData.CharacterComponent;
                    var movementSnapshotsComp = entityData.MovementSnapshotsComponent;
                    var clientPredictionSnapshotsComp = entityData.ClientPredictionSnapshotsComponent;
                    var predictedMovements = clientPredictionSnapshotsComp.PredictedMovements;

                    ref var predictedMovementData = ref ClientPredictionSnapshotsInitializerProcessor.CreateNewSnapshotData(
                        predictedMovements, currentSimulationTickNumber, movementSnapshotsComp, transformComp, modelChildTransformComp);

                    var oldPos = transformComp.Position;

                    ref var inputData = ref inputSnapshotsComp.PendingInputs.Items[inputIdx];
                    SetPredictedMovementData(simDeltaTime, movementSnapshotsComp, characterComp,
                        ref predictedMovementData, ref inputData);

                    BulletPhysicsExt.SimulateCharacter(characterComp, _simulation, simDeltaTime);
                    // Save the new position due to physics step
                    predictedMovementData.LocalPosition = entityData.TransformComponent.Position;
                    predictedMovementData.PhysicsEngineLinearVelocity = BulletPhysicsExt.GetLinearVelocity(characterComp);
                    predictedMovementData.IsGrounded = characterComp.IsGrounded;
#if DEBUG
                    Debug.WriteLine($"Resim NewPos {predictedMovementData.LocalPosition} - PIDApplied {predictedMovementData.PlayerInputSequenceNumberApplied} - MvDir {predictedMovementData.MoveDirection} - Vel {predictedMovementData.PhysicsEngineLinearVelocity} - IsGnd {predictedMovementData.IsGrounded} - JmpCmd {inputData.IsJumpButtonDown}");
                    var pm = entityData.ClientPredictionSnapshotsComponent.PredictedMovements;
                    //if (debugPrint == triggerDebugPrint1 || debugPrint == triggerDebugPrint2)
                    //{
                    //    Debug.WriteLine($"Resim NewPos {predictedMovementData.LocalPosition} - OldRealPos {oldPos} - Sim {currentSimulationTickNumber} - PIDApplied {predictedMovementData.PlayerInputSequenceNumberApplied} - MvDir {predictedMovementData.MoveDirection}");
                    //    foreach (var m in pm)
                    //    {
                    //        Debug.WriteLine($"Resim NewPos {m.LocalPosition} - PIDApplied {m.PlayerInputSequenceNumberApplied} - MvDir {m.MoveDirection} - Vel {m.PhysicsEngineLinearVelocity} - IsGnd {m.IsGrounded}");
                    //    }
                    //}
#endif
                }
            }

#if DEBUG
            //if (debugPrint == triggerDebugPrint2)
            //{
            //    for (int i = 0; i < resimulateEntities.Count; i++)
            //    {
            //        ref var entityData = ref resimulateEntities.Items[i];
            //        var transformComp = entityData.TransformComponent;
            //        var modelChildTransformComp = entityData.ModelChildEntity.Transform;
            //        var characterComp = entityData.CharacterComponent;
            //        var movementSnapshotsComp = entityData.MovementSnapshotsComponent;
            //        var clientPredictionSnapshotsComp = entityData.ClientPredictionSnapshotsComponent;
            //        var predictedMovements = clientPredictionSnapshotsComp.PredictedMovements;

            //        Debug.WriteLine($"Resim ---NEW PREDICTIONS!!");
            //        var pm = entityData.ClientPredictionSnapshotsComponent.PredictedMovements;
            //        foreach (var m in pm)
            //        {
            //            Debug.WriteLine($"Resim NewPos {m.LocalPosition} - PIDApplied {m.PlayerInputSequenceNumberApplied} - MvDir {m.MoveDirection} - Vel {m.PhysicsEngineLinearVelocity} - IsGnd {m.IsGrounded}");
            //        }
            //    }
            //    Debug.WriteLine($"------Resim END Sim {currentSimulationTickNumber}");
            //}
#endif
        }

        private static void SetPredictedMovementData(
            float simulationDeltaTime,
            MovementSnapshotsComponent movementSnapshotsComp,
            CharacterComponent characterComp,
            ref MovementSnapshotsComponent.MovementData curMovementData,
            ref InputSnapshotsComponent.InputCommandSet curInputData)
        {
            Move(movementSnapshotsComp, characterComp, ref curMovementData, ref curInputData);
            TryJump(simulationDeltaTime, movementSnapshotsComp, characterComp, ref curMovementData, ref curInputData);
        }

        private static void Move(
            MovementSnapshotsComponent movementSnapshotsComp,
            CharacterComponent characterComp,
            ref MovementSnapshotsComponent.MovementData curMovementData,
            ref InputSnapshotsComponent.InputCommandSet curInputData)
        {
            curMovementData.PlayerInputSequenceNumberApplied = curInputData.PlayerInputSequenceNumber;

            float maxSpeed = movementSnapshotsComp.MaxRunSpeed;
            // Character speed
            var newMoveDirection = new Vector3(curInputData.MoveInput.X, 0, curInputData.MoveInput.Y);

            // Allow very simple inertia to the character to make animation transitions more fluid
            curMovementData.MoveDirection = curMovementData.MoveDirection * 0.85f + newMoveDirection * 0.15f;

            curMovementData.CurrentMoveInputVelocity = curMovementData.MoveDirection * maxSpeed;
            characterComp.SetVelocity(curMovementData.CurrentMoveInputVelocity);

            // Broadcast speed as decimal percentage of the max speed
            curMovementData.MoveSpeedDecimalPercentage = curMovementData.MoveDirection.Length();
//#if DEBUG
//            Console.WriteLine($"InpProc: RunSpeed: {curMovementData.MoveSpeedDecimalPercentage} - Sim {curMovementData.SimulationTickNumber} - {curMovementData.SnapshotType}");
//#endif
            // Character orientation
            if (curMovementData.MoveDirection.LengthSquared() > 0.000001f)
            {
                curMovementData.YawOrientation = MathUtil.RadiansToDegrees((float)Math.Atan2(-curMovementData.MoveDirection.Z, curMovementData.MoveDirection.X) + MathUtil.PiOverTwo);
                curMovementData.LocalRotation = Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(curMovementData.YawOrientation), 0, 0);
                characterComp.Entity.GetChild(0).Transform.Rotation = curMovementData.LocalRotation;
            }
        }

        /// <summary>
        /// Jump makes the character jump and also accounts for the player's reaction time, making jumping feel more natural by
        ///  allowing jumps within some limit of the last time the character was on the ground
        /// </summary>
        private static void TryJump(
            float simulationDeltaTime,
            MovementSnapshotsComponent movementSnapshotsComp,
            CharacterComponent characterComp,
            ref MovementSnapshotsComponent.MovementData curMovementData,
            ref InputSnapshotsComponent.InputCommandSet curInputData)
        {
            // Check if conditions allow the character to jump
            if (movementSnapshotsComp.JumpReactionThreshold <= 0)
            {
                // No reaction threshold. The character can only jump if grounded
                if (!characterComp.IsGrounded)
                {
                    curMovementData.IsGrounded = false;
                    return;
                }
            }
            else
            {
                // If there is still enough time left for jumping allow the character to jump even when not grounded
                if (curMovementData.JumpReactionRemaining > 0)
                {
                    curMovementData.JumpReactionRemaining -= simulationDeltaTime;
                }

                // If the character on the ground reset the jumping reaction time
                if (characterComp.IsGrounded)
                {
                    curMovementData.JumpReactionRemaining = movementSnapshotsComp.JumpReactionThreshold;
                }

                // If there is no more reaction time left don't allow the character to jump
                if (curMovementData.JumpReactionRemaining <= 0)
                {
                    //PlayerController.IsGroundedEventKey.Broadcast(characterComp.IsGrounded);
                    curMovementData.IsGrounded = characterComp.IsGrounded;
                    return;
                }
            }

            // If the player didn't press a jump button we don't need to jump
            if (!curInputData.IsJumpButtonDown)
            {
                //PlayerController.IsGroundedEventKey.Broadcast(true);
                curMovementData.IsGrounded = true;
                return;
            }

            // Jump
            curMovementData.JumpReactionRemaining = 0;
            characterComp.Jump();

            // Broadcast that the character is jumping!
            //PlayerController.IsGroundedEventKey.Broadcast(false);
            curMovementData.IsGrounded = false;
        }

        internal class AssociatedData
        {
            internal TransformComponent TransformComponent;
            // This component is the physics representation of a controllable character
            internal CharacterComponent CharacterComponent;
            internal Entity ModelChildEntity;
            internal InputSnapshotsComponent InputSnapshotsComponent;

            internal ClientPredictionSnapshotsComponent ClientPredictionSnapshotsComponent;
        }

        public readonly struct PredictMovementEntityData
        {
            public readonly TransformComponent TransformComponent;
            public readonly CharacterComponent CharacterComponent;
            public readonly Entity ModelChildEntity;
            public readonly InputSnapshotsComponent InputSnapshotsComponent;
            public readonly MovementSnapshotsComponent MovementSnapshotsComponent;
            public readonly ClientPredictionSnapshotsComponent ClientPredictionSnapshotsComponent;

            public PredictMovementEntityData(
                TransformComponent transformComponent,
                CharacterComponent character,
                Entity modelChildEntity,
                InputSnapshotsComponent inputSnapshotsComponent,
                MovementSnapshotsComponent movementSnapshotsComponent,
                ClientPredictionSnapshotsComponent clientPredictionSnapshotsComponent)
            {
                TransformComponent = transformComponent;
                CharacterComponent = character;
                ModelChildEntity = modelChildEntity;
                InputSnapshotsComponent = inputSnapshotsComponent;
                MovementSnapshotsComponent = movementSnapshotsComponent;
                ClientPredictionSnapshotsComponent = clientPredictionSnapshotsComponent;
            }
        }
    }
}
