using MultiplayerExample.Core;
using MultiplayerExample.Engine;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Physics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MultiplayerExample.Network.SnapshotStores
{
    class MovementSnapshotsInputProcessor : EntityProcessor<MovementSnapshotsComponent, MovementSnapshotsInputProcessor.AssociatedData>,
        IInGameProcessor, IPreUpdateProcessor
    {
        private SceneSystem _sceneSystem;
        private Simulation _simulation;
        private GameClockManager _gameClockManager;
        private GameEngineContext _gameEngineContext;
        private IGameNetworkService _networkService;
        private PhysicsProcessor _physicsProcessor;

        public bool IsEnabled { get; set; }

        public MovementSnapshotsInputProcessor() : base(typeof(TransformComponent), typeof(CharacterComponent), typeof(InputSnapshotsComponent))
        {
            Order = 10;         // Ensure this occurs after MovementSnapshotsProcessor & InputSnapshotsInGameInputProcessor
            // Not using Enabled property, because that completely disables the processor, where it doesn't even pick up newly added entities
            IsEnabled = true;
        }

        protected override void OnSystemAdd()
        {
            _sceneSystem = Services.GetSafeServiceAs<SceneSystem>();

            _gameClockManager = Services.GetSafeServiceAs<GameClockManager>();
            _gameEngineContext = Services.GetService<GameEngineContext>();
            _networkService = Services.GetService<IGameNetworkService>();
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
            if (!_networkService.IsGameHost)
            {
                // Client sets predicted movement list from input data (movement data will be set when it receives it from the server).
                foreach (var kv in ComponentDatas)
                {
                    var data = kv.Value;
                    var clientPredictionSnapshotsComp = data.ClientPredictionSnapshotsComponent;
                    if (clientPredictionSnapshotsComp != null)
                    {
                        var movementSnapshotsComp = kv.Key;
                        var inputSnapshotsComp = data.InputSnapshotsComponent;
                        var characterComp = data.CharacterComponent;
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
                            predictedMovements, simTickNumber, movementSnapshotsComp, data.TransformComponent);
                        SetMovementDataFromInput(simDeltaTime, movementSnapshotsComp, characterComp,
                            ref predictedMovementData, ref curInputData);
                    }
                }
            }
            else
            {
                // Server sets the movement data directly from input data.
                foreach (var kv in ComponentDatas)
                {
                    var movementSnapshotsComp = kv.Key;
                    var data = kv.Value;
                    var inputSnapshotsComp = data.InputSnapshotsComponent;
                    var movementFindResult = movementSnapshotsComp.SnapshotStore.TryFindSnapshot(simTickNumber);
                    var inputFindResult = inputSnapshotsComp.SnapshotStore.TryFindSnapshot(simTickNumber);
                    if (!movementFindResult.IsFound || !inputFindResult.IsFound)
                    {
#if DEBUG
                        Debug.WriteLine($"Svr SkippingPlayerInput: Sim {simTickNumber}");
#endif
                        continue;
                    }

                    var characterComp = data.CharacterComponent;
                    ref var curMovementData = ref movementFindResult.Result;
                    ref var curInputData = ref inputFindResult.Result;
                    // Update character's velocity (actual position change is done in the physics simulation)
                    SetMovementDataFromInput(simDeltaTime, movementSnapshotsComp, characterComp,
                        ref curMovementData, ref curInputData);

#if DEBUG
                    //if (curInputData.MoveInput.LengthSquared() > 0)
                    //{
                    //    Debug.WriteLine($"SvrMovementSnapshotsInputProcessor.Update: {curInputData.MoveInput} - Sim {simTickNumber}");
                    //}
#endif
                }
            }
        }

        internal void Resimulate(SimulationTickNumber currentSimulationTickNumber, List<PredictMovementEntityData> resimulateEntities)
        {
            _physicsProcessor ??= _sceneSystem.SceneInstance.GetProcessor<PhysicsProcessor>();
            _simulation ??= _physicsProcessor?.Simulation;
            Debug.Assert(_physicsProcessor != null, "Physics Processor needs to be created.");
            Debug.Assert(_simulation != null, "Physics Simulation needs to be created.");
            Debug.Assert(_gameEngineContext.IsClient, "Server engine should not be resimulating entities.");

            var resimulateEntitiesSpan = CollectionsMarshal.AsSpan(resimulateEntities);
            int maxResimInputCount = 0;
            // Reset the initial positions to the start of the simulation time
            for (int i = 0; i < resimulateEntitiesSpan.Length; i++)
            {
                ref var entityData = ref resimulateEntitiesSpan[i];

                var transformComp = entityData.TransformComponent;
                var characterComp = entityData.CharacterComponent;
                var movementSnapshotsComp = entityData.MovementSnapshotsComponent;

                Debug.Assert(!movementSnapshotsComp.SnapshotStore.IsEmpty);

                ref var movementData = ref movementSnapshotsComp.SnapshotStore.GetLatest();

#if DEBUG
                {
                    var pmmm = entityData.ClientPredictionSnapshotsComponent.PredictedMovements;
                    var confirmedPID = movementData.PlayerInputSequenceNumberApplied;
                    var iindex = pmmm.FindIndex(x => x.PlayerInputSequenceNumberApplied == confirmedPID);
                    if (iindex >= 0)
                    {
                        var mmm = pmmm[iindex];
                        if (mmm.LocalPosition != movementData.LocalPosition)
                        {
                            // Misprediction.... TODO?
                        }
                    }
                }
#endif
                transformComp.Position = movementData.LocalPosition;
                //modelChildTransform.Rotation  movementData.LocalRotation;
                BulletPhysicsExt.SetLinearVelocity(characterComp, ref movementData.PhysicsEngineLinearVelocity);
                characterComp.SetVelocity(Vector3.Zero);    // Need to set to zero here because even simulating with zero delta time moves the character...
                characterComp.UpdatePhysicsTransformation(forceUpdateTransform: true);        // Update in the physics engine
                BulletPhysicsExt.SimulateCharacter(characterComp, _simulation, deltaTimeInSeconds: 0);      // We need to 'resimulate' with zero delta time to ensure character IsGround or !IsGround is reset properly

                // Find the max number of resimulations we'll need to run
                var inputSnapshotsComp = entityData.InputSnapshotsComponent;
                maxResimInputCount = Math.Max(maxResimInputCount, inputSnapshotsComp.PendingInputs.Count);

#if DEBUG
                //var pm = entityData.ClientPredictionSnapshotsComponent.PredictedMovements;
                //Debug.WriteLine($"ResimPrep --- Show prev predictions:");
                //foreach (var m in pm)
                //{
                //    Debug.WriteLine($"Resim Pos {m.LocalPosition} - PIDApplied {m.PlayerInputSequenceNumberApplied} - MvDir {m.MoveDirection} - InpVel {m.CurrentMoveInputVelocity} - PhyVel {m.PhysicsEngineLinearVelocity} - Yaw {m.YawOrientation} - IsGnd {m.IsGrounded}");
                //}
                //for (int jjjj = movementSnapshotsComp.SnapshotStore.Count - 1; jjjj >= 0; jjjj--)
                //{
                //    ref var m = ref movementSnapshotsComp.SnapshotStore.GetPrevious(jjjj);
                //    Debug.WriteLine($"Resim NewPos {m.LocalPosition} - PIDApplied {m.PlayerInputSequenceNumberApplied} - MvDir {m.MoveDirection} - InpVel {m.CurrentMoveInputVelocity} - PhyVel {m.PhysicsEngineLinearVelocity} - Yaw {m.YawOrientation}");
                //}
#endif

                // Remove old predictions
                var clientPredictionSnapshotsComp = entityData.ClientPredictionSnapshotsComponent;
                clientPredictionSnapshotsComp.PredictedMovements.Clear();
                // Add a fake prediction values, which is actually just the most recent two server values.
                // This is done so NetworkMovementViewProcessor will have two values to use for interpolation
                // even if all input predictions have been consumed.
                var predictedMovements = clientPredictionSnapshotsComp.PredictedMovements;
                int movementCopyCount = Math.Min(2, movementSnapshotsComp.SnapshotStore.Count);
                for (int mvmtIndex = movementCopyCount - 1; mvmtIndex >= 0; mvmtIndex--)
                {
                    ref var predictedMovementData = ref ClientPredictionSnapshotsInitializerProcessor.CreateNewSnapshotData(
                        predictedMovements, currentSimulationTickNumber - 1, movementSnapshotsComp, transformComp);
                    // Copy the data directly over
                    predictedMovementData = movementSnapshotsComp.SnapshotStore.GetPrevious(mvmtIndex);
                }
            }

            float simDeltaTime = _simulation.FixedTimeStep;
            for (int inputIdx = 0; inputIdx < maxResimInputCount; inputIdx++)
            {
                for (int i = 0; i < resimulateEntitiesSpan.Length; i++)
                {
                    ref var entityData = ref resimulateEntitiesSpan[i];
                    var inputSnapshotsComp = entityData.InputSnapshotsComponent;
                    if (inputIdx >= inputSnapshotsComp.PendingInputs.Count)
                    {
                        // If multiple entities require resimulation, technically they should all have the same
                        // number of unacked inputs, but maybe this won't be true if further code optimization is done?
                        continue;
                    }

                    var transformComp = entityData.TransformComponent;
                    var characterComp = entityData.CharacterComponent;
                    var movementSnapshotsComp = entityData.MovementSnapshotsComponent;
                    var clientPredictionSnapshotsComp = entityData.ClientPredictionSnapshotsComponent;
                    var predictedMovements = clientPredictionSnapshotsComp.PredictedMovements;

                    ref var predictedMovementData = ref ClientPredictionSnapshotsInitializerProcessor.CreateNewSnapshotData(
                        predictedMovements, currentSimulationTickNumber, movementSnapshotsComp, transformComp);

                    var oldPos = transformComp.Position;

                    var pendingInputsSpan = CollectionsMarshal.AsSpan(inputSnapshotsComp.PendingInputs);
                    ref var inputData = ref pendingInputsSpan[inputIdx];
#if DEBUG
                    //Debug.WriteLine($"InpProc: RunSpeed: {curMovementData.MoveSpeedDecimalPercentage} - Sim {curMovementData.SimulationTickNumber} - {curMovementData.SnapshotType}");
#endif
                    SetMovementDataFromInput(simDeltaTime, movementSnapshotsComp, characterComp,
                        ref predictedMovementData, ref inputData);
                    // Do physics step on the character
                    BulletPhysicsExt.SimulateCharacter(characterComp, _simulation, simDeltaTime);
                    // Save the new position due to physics step
                    predictedMovementData.LocalPosition = entityData.TransformComponent.Position;
                    predictedMovementData.PhysicsEngineLinearVelocity = BulletPhysicsExt.GetLinearVelocity(characterComp);
                    predictedMovementData.IsGrounded = characterComp.IsGrounded;
                }
            }

#if DEBUG
            //{
            //    for (int i = 0; i < resimulateEntities.Count; i++)
            //    {
            //        ref var entityData = ref resimulateEntities.Items[i];
            //        var transformComp = entityData.TransformComponent;
            //        var characterComp = entityData.CharacterComponent;
            //        var movementSnapshotsComp = entityData.MovementSnapshotsComponent;
            //        var clientPredictionSnapshotsComp = entityData.ClientPredictionSnapshotsComponent;
            //        var predictedMovements = clientPredictionSnapshotsComp.PredictedMovements;

            //        Debug.WriteLine($"Resim ---NEW PREDICTION---");
            //        var pm = entityData.ClientPredictionSnapshotsComponent.PredictedMovements;
            //        foreach (var m in pm)
            //        {
            //            Debug.WriteLine($"Resim NewPos {m.LocalPosition} - PIDApplied {m.PlayerInputSequenceNumberApplied} - MvDir {m.MoveDirection} - InpVel {m.CurrentMoveInputVelocity} - PhyVel {m.PhysicsEngineLinearVelocity} - Yaw {m.YawOrientation} - IsGnd {m.IsGrounded}");
            //        }
            //    }
            //    Debug.WriteLine($"------Resim END Sim {currentSimulationTickNumber}");
            //}
#endif
        }

        private static void SetMovementDataFromInput(
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
#if DEBUG
            var prevMoveDir = curMovementData.MoveDirection;
#endif

            // Allow very simple inertia to the character to make animation transitions more fluid
            curMovementData.MoveDirection = curMovementData.MoveDirection * 0.85f + newMoveDirection * 0.15f;

            curMovementData.CurrentMoveInputVelocity = curMovementData.MoveDirection * maxSpeed;
#if DEBUG
            //Debug.WriteLine($"InpProc: RunSpeed: {curMovementData.MoveSpeedDecimalPercentage} - Sim {curMovementData.SimulationTickNumber} - {curMovementData.SnapshotType}");
#endif

            characterComp.SetVelocity(curMovementData.CurrentMoveInputVelocity);

            // Broadcast speed as decimal percentage of the max speed
            curMovementData.MoveSpeedDecimalPercentage = curMovementData.MoveDirection.Length();
#if DEBUG
            //Debug.WriteLine($"InpProc: RunSpeed: {curMovementData.MoveSpeedDecimalPercentage} - Sim {curMovementData.SimulationTickNumber} - Inp {curInputData.MoveInput} - PID {curInputData.PlayerInputSequenceNumber} - PrevMvDir {prevMoveDir} - NextMvDir {curMovementData.MoveDirection} - NextInpVel {curMovementData.CurrentMoveInputVelocity} - PrevPos {curMovementData.LocalPosition}");
#endif
            // Character orientation
            if (curMovementData.MoveDirection.LengthSquared() > 0.000001f)
            {
                curMovementData.SetRotationFromFacingDirection(curMovementData.MoveDirection);
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
            internal InputSnapshotsComponent InputSnapshotsComponent;

            internal ClientPredictionSnapshotsComponent ClientPredictionSnapshotsComponent;
        }

        public readonly struct PredictMovementEntityData
        {
            public readonly TransformComponent TransformComponent;
            public readonly CharacterComponent CharacterComponent;
            public readonly InputSnapshotsComponent InputSnapshotsComponent;
            public readonly MovementSnapshotsComponent MovementSnapshotsComponent;
            public readonly ClientPredictionSnapshotsComponent ClientPredictionSnapshotsComponent;

            public PredictMovementEntityData(
                TransformComponent transformComponent,
                CharacterComponent character,
                InputSnapshotsComponent inputSnapshotsComponent,
                MovementSnapshotsComponent movementSnapshotsComponent,
                ClientPredictionSnapshotsComponent clientPredictionSnapshotsComponent)
            {
                TransformComponent = transformComponent;
                CharacterComponent = character;
                InputSnapshotsComponent = inputSnapshotsComponent;
                MovementSnapshotsComponent = movementSnapshotsComponent;
                ClientPredictionSnapshotsComponent = clientPredictionSnapshotsComponent;
            }
        }
    }
}
