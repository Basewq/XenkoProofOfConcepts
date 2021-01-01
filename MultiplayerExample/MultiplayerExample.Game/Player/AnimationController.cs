// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using MultiplayerExample.Network;
using MultiplayerExample.Network.SnapshotStores;
using Stride.Animations;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using System;
using System.Diagnostics;

namespace MultiplayerExample.Player
{
    public class AnimationController : SyncScript, IBlendTreeBuilder
    {
        [Display("Animation Component")]
        public AnimationComponent AnimationComponent { get; set; }

        [Display("Idle")]
        public AnimationClip AnimationIdle { get; set; }

        [Display("Walk")]
        public AnimationClip AnimationWalk { get; set; }

        [Display("Run")]
        public AnimationClip AnimationRun { get; set; }

        [Display("Jump")]
        public AnimationClip AnimationJumpStart { get; set; }

        [Display("Airborne")]
        public AnimationClip AnimationJumpMid { get; set; }

        [Display("Landing")]
        public AnimationClip AnimationJumpEnd { get; set; }

        [DataMemberRange(0, 1, 0.01, 0.1, 3)]
        [Display("Walk Threshold")]
        public float WalkThreshold { get; set; } = 0.25f;

        [Display("Time Scale")]
        public double TimeFactor { get; set; } = 1;

        private AnimationClipEvaluator animEvaluatorIdle;
        private AnimationClipEvaluator animEvaluatorWalk;
        private AnimationClipEvaluator animEvaluatorRun;
        private AnimationClipEvaluator animEvaluatorJumpStart;
        private AnimationClipEvaluator animEvaluatorJumpMid;
        private AnimationClipEvaluator animEvaluatorJumpEnd;
        private double currentTime = 0;

        // Idle-Walk-Run lerp
        private AnimationClipEvaluator animEvaluatorWalkLerp1;
        private AnimationClipEvaluator animEvaluatorWalkLerp2;
        private AnimationClip animationClipWalkLerp1;
        private AnimationClip animationClipWalkLerp2;
        private float walkLerpFactor = 0.5f;

        // Internal state
        private bool isGrounded = false;
        private AnimationState state = AnimationState.Airborne;

        private float runSpeed;

        private GameClockManager _gameClockManager;
        private IGameNetworkService _networkService;

        // Components from the networked entity
        private NetworkEntityComponent _networkEntityComponent;
        private MovementSnapshotsComponent _movementSnapshotsComponent;
        private ClientPredictionSnapshotsComponent _clientPredictionSnapshotsComponent;     // Optional component

        public override void Start()
        {
            base.Start();

            var parentEntity = Entity.GetParent();
            Debug.Assert(parentEntity != null);

            var networkEntityViewComp = parentEntity.Get<NetworkEntityViewComponent>();
            var networkedEntity = networkEntityViewComp.NetworkedEntity;
            _networkEntityComponent = networkedEntity.Get<NetworkEntityComponent>();
            Debug.Assert(_networkEntityComponent != null);
            _movementSnapshotsComponent = networkedEntity.Get<MovementSnapshotsComponent>();
            Debug.Assert(_movementSnapshotsComponent != null);
            _clientPredictionSnapshotsComponent = networkedEntity.Get<ClientPredictionSnapshotsComponent>();

            _gameClockManager = Services.GetSafeServiceAs<GameClockManager>();
            _networkService = Services.GetService<IGameNetworkService>();

            if (AnimationComponent == null)
                throw new InvalidOperationException("The animation component is not set");

            if (AnimationIdle == null)
                throw new InvalidOperationException("Idle animation is not set");

            if (AnimationWalk == null)
                throw new InvalidOperationException("Walking animation is not set");

            if (AnimationRun == null)
                throw new InvalidOperationException("Running animation is not set");

            if (AnimationJumpStart == null)
                throw new InvalidOperationException("Jumping animation is not set");

            if (AnimationJumpMid == null)
                throw new InvalidOperationException("Airborne animation is not set");

            if (AnimationJumpEnd == null)
                throw new InvalidOperationException("Landing animation is not set");

            // By setting a custom blend tree builder we can override the default behavior of the animation system
            //  Instead, BuildBlendTree(FastList<AnimationOperation> blendStack) will be called each frame
            AnimationComponent.BlendTreeBuilder = this;

            animEvaluatorIdle = AnimationComponent.Blender.CreateEvaluator(AnimationIdle);
            animEvaluatorWalk = AnimationComponent.Blender.CreateEvaluator(AnimationWalk);
            animEvaluatorRun = AnimationComponent.Blender.CreateEvaluator(AnimationRun);
            animEvaluatorJumpStart = AnimationComponent.Blender.CreateEvaluator(AnimationJumpStart);
            animEvaluatorJumpMid = AnimationComponent.Blender.CreateEvaluator(AnimationJumpMid);
            animEvaluatorJumpEnd = AnimationComponent.Blender.CreateEvaluator(AnimationJumpEnd);

            // Initial walk lerp
            walkLerpFactor = 0;
            animEvaluatorWalkLerp1 = animEvaluatorIdle;
            animEvaluatorWalkLerp2 = animEvaluatorWalk;
            animationClipWalkLerp1 = AnimationIdle;
            animationClipWalkLerp2 = AnimationWalk;
        }

        public override void Cancel()
        {
            AnimationComponent.Blender.ReleaseEvaluator(animEvaluatorIdle);
            AnimationComponent.Blender.ReleaseEvaluator(animEvaluatorWalk);
            AnimationComponent.Blender.ReleaseEvaluator(animEvaluatorRun);
            AnimationComponent.Blender.ReleaseEvaluator(animEvaluatorJumpStart);
            AnimationComponent.Blender.ReleaseEvaluator(animEvaluatorJumpMid);
            AnimationComponent.Blender.ReleaseEvaluator(animEvaluatorJumpEnd);
        }

        private void UpdateWalking()
        {
            if (runSpeed < WalkThreshold)
            {
                walkLerpFactor = runSpeed / WalkThreshold;
                walkLerpFactor = (float)Math.Sqrt(walkLerpFactor);  // Idle-Walk blend looks really werid, so skew the factor towards walking
                animEvaluatorWalkLerp1 = animEvaluatorIdle;
                animEvaluatorWalkLerp2 = animEvaluatorWalk;
                animationClipWalkLerp1 = AnimationIdle;
                animationClipWalkLerp2 = AnimationWalk;
            }
            else
            {
                walkLerpFactor = (runSpeed - WalkThreshold) / (1.0f - WalkThreshold);
                animEvaluatorWalkLerp1 = animEvaluatorWalk;
                animEvaluatorWalkLerp2 = animEvaluatorRun;
                animationClipWalkLerp1 = AnimationWalk;
                animationClipWalkLerp2 = AnimationRun;
            }

            // Original code used Game.DrawTime but script may run multiple times in a single update so must use UpdateTime
            var time = Game.UpdateTime;
            // This update function will account for animation with different durations, keeping a current time relative to the blended maximum duration
            long blendedMaxDuration =
                (long)MathUtil.Lerp(animationClipWalkLerp1.Duration.Ticks, animationClipWalkLerp2.Duration.Ticks, walkLerpFactor);

            var currentTicks = TimeSpan.FromTicks((long)(currentTime * blendedMaxDuration));

            currentTicks = blendedMaxDuration == 0
                ? TimeSpan.Zero
                : TimeSpan.FromTicks((currentTicks.Ticks + (long)(time.Elapsed.Ticks * TimeFactor)) %
                                     blendedMaxDuration);

            currentTime = ((double)currentTicks.Ticks / (double)blendedMaxDuration);
        }

        private void UpdateJumping()
        {
            // Original code used Game.DrawTime but script may run multiple times in a single update so must use UpdateTime
            var time = Game.UpdateTime;
            var speedFactor = 1;
            var currentTicks = TimeSpan.FromTicks((long)(currentTime * AnimationJumpStart.Duration.Ticks));
            var updatedTicks = currentTicks.Ticks + (long)(time.Elapsed.Ticks * TimeFactor * speedFactor);

            if (updatedTicks < AnimationJumpStart.Duration.Ticks)
            {
                currentTicks = TimeSpan.FromTicks(updatedTicks);
                currentTime = ((double)currentTicks.Ticks / (double)AnimationJumpStart.Duration.Ticks);
            }
            else
            {
                state = AnimationState.Airborne;
                currentTime = 0;
                UpdateAirborne();
            }
        }

        private void UpdateAirborne()
        {
            // Original code used Game.DrawTime but script may run multiple times in a single update so must use UpdateTime
            var time = Game.UpdateTime;
            var currentTicks = TimeSpan.FromTicks((long)(currentTime * AnimationJumpMid.Duration.Ticks));
            currentTicks = TimeSpan.FromTicks((currentTicks.Ticks + (long)(time.Elapsed.Ticks * TimeFactor)) %
                                     AnimationJumpMid.Duration.Ticks);
            currentTime = ((double)currentTicks.Ticks / (double)AnimationJumpMid.Duration.Ticks);
        }

        private void UpdateLanding()
        {
            // Original code used Game.DrawTime but script may run multiple times in a single update so must use UpdateTime
            var time = Game.UpdateTime;
            var speedFactor = 1;
            var currentTicks = TimeSpan.FromTicks((long)(currentTime * AnimationJumpEnd.Duration.Ticks));
            var updatedTicks = currentTicks.Ticks + (long) (time.Elapsed.Ticks * TimeFactor * speedFactor);

            if (updatedTicks < AnimationJumpEnd.Duration.Ticks)
            {
                currentTicks = TimeSpan.FromTicks(updatedTicks);
                currentTime = ((double)currentTicks.Ticks / (double)AnimationJumpEnd.Duration.Ticks);
            }
            else
            {
                state = AnimationState.Walking;
                currentTime = 0;
                UpdateWalking();
            }
        }

        public override void Update()
        {
            bool isGroundedNewValue;
            if (_networkEntityComponent.IsLocalEntity && _clientPredictionSnapshotsComponent != null && !_networkService.IsGameHost)
            {
                var predictedMovements = _clientPredictionSnapshotsComponent.PredictedMovements;
                if (predictedMovements.Count <= 0)
                {
                    return;
                }
                ref var curMovementData = ref predictedMovements.Items[predictedMovements.Count - 1];
                // State control
                runSpeed = curMovementData.MoveSpeedDecimalPercentage;

                isGroundedNewValue = curMovementData.IsGrounded;
            }
            else
            {
                var animTime = _gameClockManager.SimulationClock.TotalTime;
                if (!_networkEntityComponent.IsLocalEntity)
                {
                    animTime -= _gameClockManager.RemoteEntityRenderTimeDelay;
                }
                var simTickNumber = GameClockManager.CalculateSimulationTickNumber(animTime);
                var snapshotStore = _movementSnapshotsComponent.SnapshotStore;
                var movementFindResult = snapshotStore.TryFindSnapshotClosestEqualOrLessThan(simTickNumber);
                if (!movementFindResult.IsFound)
                {
                    return;
                }
                ref var curMovementData = ref movementFindResult.Result;
                // State control
                runSpeed = curMovementData.MoveSpeedDecimalPercentage;

                isGroundedNewValue = curMovementData.IsGrounded;
#if DEBUG
                //Debug.WriteLine($"AnimCtrl-Remote: RunSpeed: {runSpeed} - Sim {curMovementData.SimulationTickNumber} - FindSim {simTickNumber} - {curMovementData.SnapshotType} - Sim {curMovementData.CurrentMoveVelocity}");
                //Console.WriteLine($"AnimCtrl-Remote: RunSpeed: {runSpeed} - Sim {curMovementData.SimulationTickNumber} - FindSim {simTickNumber} - {curMovementData.SnapshotType}");
#endif
            }
            if (isGrounded != isGroundedNewValue)
            {
                currentTime = 0;
                isGrounded = isGroundedNewValue;
                state = (isGrounded) ? AnimationState.Landing : AnimationState.Jumping;
            }

            switch (state)
            {
                case AnimationState.Walking:  UpdateWalking();  break;
                case AnimationState.Jumping:  UpdateJumping();  break;
                case AnimationState.Airborne: UpdateAirborne(); break;
                case AnimationState.Landing:  UpdateLanding();  break;
            }
        }

        /// <summary>
        /// BuildBlendTree is called every frame from the animation system when the <see cref="AnimationComponent"/> needs to be evaluated
        /// It overrides the default behavior of the <see cref="AnimationComponent"/> by setting a custom blend tree
        /// </summary>
        /// <param name="blendStack">The stack of animation operations to be blended</param>
        public void BuildBlendTree(FastList<AnimationOperation> blendStack)
        {
            switch (state)
            {
                case AnimationState.Walking:
                    {
                        // Note! The tree is laid out as a stack and has to be flattened before returning it to the animation system!
                        blendStack.Add(AnimationOperation.NewPush(animEvaluatorWalkLerp1,
                            TimeSpan.FromTicks((long)(currentTime * animationClipWalkLerp1.Duration.Ticks))));
                        blendStack.Add(AnimationOperation.NewPush(animEvaluatorWalkLerp2,
                            TimeSpan.FromTicks((long)(currentTime * animationClipWalkLerp2.Duration.Ticks))));
                        blendStack.Add(AnimationOperation.NewBlend(CoreAnimationOperation.Blend, walkLerpFactor));
                    }
                    break;

                case AnimationState.Jumping:
                    {
                        blendStack.Add(AnimationOperation.NewPush(animEvaluatorJumpStart,
                            TimeSpan.FromTicks((long)(currentTime * AnimationJumpStart.Duration.Ticks))));
                    }
                    break;

                case AnimationState.Airborne:
                    {
                        blendStack.Add(AnimationOperation.NewPush(animEvaluatorJumpMid,
                            TimeSpan.FromTicks((long)(currentTime * AnimationJumpMid.Duration.Ticks))));
                    }
                    break;

                case AnimationState.Landing:
                    {
                        blendStack.Add(AnimationOperation.NewPush(animEvaluatorJumpEnd,
                            TimeSpan.FromTicks((long)(currentTime * AnimationJumpEnd.Duration.Ticks))));
                    }
                    break;
            }
        }

        enum AnimationState
        {
            Walking,
            Jumping,
            Airborne,
            Landing,
        }
    }
}
