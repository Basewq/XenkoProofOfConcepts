using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xenko.Core.Collections;
using Xenko.Core.Diagnostics;
using Xenko.Physics;
using Xenko.Rendering;
using Matrix = Xenko.Core.Mathematics.Matrix;
using Quaternion = Xenko.Core.Mathematics.Quaternion;
using Vector3 = Xenko.Core.Mathematics.Vector3;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    public class BepuSimulation : IDisposable
    {
        const CollisionFilterGroups DefaultGroup = CollisionFilterGroups.DefaultFilter;
        const CollisionFilterGroupFlags DefaultFlags = CollisionFilterGroupFlags.AllFilter;

        /// <summary>
        /// List pool that can be used to store <see cref="RaycastPenetrating"/> or <see cref="ShapeSweepPenetrating"/> results.
        /// </summary>
        public static readonly ObjectPool<FastList<BepuHitResult>> HitResultListPool = new DefaultObjectPool<FastList<BepuHitResult>>(BepuHitResultListPooledObjectPolicy.Instance);

        // The pool containing the collision data used by the simulation.
        // Note that the buffer pool used by the simulation is not considered to be *owned* by the simulation. The simulation merely uses the pool.
        // Disposing the simulation will not dispose or clear the buffer pool.
        internal readonly BufferPool BufferPool = new BufferPool();
        internal readonly BepuPhysics.Simulation NativeBepuSimulation;

        private readonly BepuPhysicsProcessor processor;

        private NarrowPhaseCallbacks narrowPhaseCallbacks;
        private PoseIntegratorCallbacks poseIntegratorCallbacks;


        //private readonly BulletSharp.DiscreteDynamicsWorld discreteDynamicsWorld;

        private readonly Dictionary<CollidableReferenceKey, BepuPhysicsComponent> collidableReferenceToComponent = new Dictionary<CollidableReferenceKey, BepuPhysicsComponent>();
        private readonly Dictionary<int, BepuPhysicsSkinnedComponentBase> bodyHandleIdToComponent = new Dictionary<int, BepuPhysicsSkinnedComponentBase>();

        internal readonly bool CanCcd;

#if DEBUG
        private static readonly Logger Log = GlobalLogger.GetLogger(typeof(BepuSimulation).FullName);
#endif

        internal readonly ContinuousDetectionSettings ContinuousDetectionSettings;

        private bool continuousCollisionDetection;
        public bool ContinuousCollisionDetection
        {
            get
            {
                if (!CanCcd)
                {
                    throw new Exception("ContinuousCollisionDetection must be enabled at physics engine initialization using the proper flag.");
                }

                return continuousCollisionDetection;
            }
            set
            {
                if (!CanCcd)
                {
                    throw new Exception("ContinuousCollisionDetection must be enabled at physics engine initialization using the proper flag.");
                }

                continuousCollisionDetection = value;
            }
        }

        /// <summary>
        /// Totally disable the simulation if set to true
        /// </summary>
        public static bool DisableSimulation = false;

        public delegate BepuPhysicsEngineFlags OnSimulationCreationDelegate();

        /// <summary>
        /// Temporary solution to inject engine flags
        /// </summary>
        public static OnSimulationCreationDelegate OnSimulationCreation;

        /// <summary>
        /// Initializes the Physics engine using the specified flags.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="configuration"></param>
        /// <exception cref="System.NotImplementedException">SoftBody processing is not yet available</exception>
        internal BepuSimulation(BepuPhysicsProcessor processor, BepuPhysicsSettings configuration)
        {
            this.processor = processor;

            if (configuration.Flags == BepuPhysicsEngineFlags.None)
            {
                configuration.Flags = OnSimulationCreation?.Invoke() ?? configuration.Flags;
            }

            MaxSubSteps = configuration.MaxSubSteps;
            FixedTimeStep = configuration.FixedTimeStep;
            AngularIntegrationMode = configuration.AngularIntegrationMode;

            narrowPhaseCallbacks = new NarrowPhaseCallbacks(this);
            poseIntegratorCallbacks = new PoseIntegratorCallbacks(this);
            NativeBepuSimulation = BepuPhysics.Simulation.Create(BufferPool, narrowPhaseCallbacks, poseIntegratorCallbacks);
            NativeBepuSimulation.Deterministic = true;

            if (configuration.Flags.HasFlag(BepuPhysicsEngineFlags.SoftBodySupport))
            {
                throw new NotImplementedException("SoftBody processing is not yet available");
            }

            //if (discreteDynamicsWorld != null)
            //{
            //    solverInfo = discreteDynamicsWorld.SolverInfo; //we are required to keep this reference, or the GC will mess up
            //    continuousDetectionSettings = discreteDynamicsWorld.DispatchInfo;
            //
            //    solverInfo.SolverMode |= BulletSharp.SolverModes.CacheFriendly; //todo test if helps with performance or not

                if (configuration.Flags.HasFlag(BepuPhysicsEngineFlags.ContinuousCollisionDetection))
                {
                    CanCcd = true;
                    //solverInfo.SolverMode |= BulletSharp.SolverModes.Use2FrictionDirections | BulletSharp.SolverModes.RandomizeOrder;
                    continuousCollisionDetection = true;
                }
            //}
        }

        private readonly List<BepuCollision> newCollisionsCache = new List<BepuCollision>();
        private readonly List<BepuCollision> removedCollisionsCache = new List<BepuCollision>();
        private readonly List<BepuContactPoint> newContactsFastCache = new List<BepuContactPoint>();
        private readonly List<BepuContactPoint> updatedContactsCache = new List<BepuContactPoint>();
        private readonly List<BepuContactPoint> removedContactsCache = new List<BepuContactPoint>();

        //private ProfilingState contactsProfilingState;

        private readonly Dictionary<BepuContactPoint, BepuCollision> contactToCollision = new Dictionary<BepuContactPoint, BepuCollision>();

        internal void SendEvents()
        {
            foreach (var collision in newCollisionsCache)
            {
                while (collision.ColliderA.NewPairChannel.Balance < 0)
                {
                    collision.ColliderA.NewPairChannel.Send(collision);
                }

                while (collision.ColliderB.NewPairChannel.Balance < 0)
                {
                    collision.ColliderB.NewPairChannel.Send(collision);
                }
            }

            foreach (var collision in removedCollisionsCache)
            {
                while (collision.ColliderA.PairEndedChannel.Balance < 0)
                {
                    collision.ColliderA.PairEndedChannel.Send(collision);
                }

                while (collision.ColliderB.PairEndedChannel.Balance < 0)
                {
                    collision.ColliderB.PairEndedChannel.Send(collision);
                }
            }

            foreach (var contactPoint in newContactsFastCache)
            {
                BepuCollision collision;
                if (contactToCollision.TryGetValue(contactPoint, out collision))
                {
                    while (collision.NewContactChannel.Balance < 0)
                    {
                        collision.NewContactChannel.Send(contactPoint);
                    }
                }
            }

            foreach (var contactPoint in updatedContactsCache)
            {
                BepuCollision collision;
                if (contactToCollision.TryGetValue(contactPoint, out collision))
                {
                    while (collision.ContactUpdateChannel.Balance < 0)
                    {
                        collision.ContactUpdateChannel.Send(contactPoint);
                    }
                }
            }

            foreach (var contactPoint in removedContactsCache)
            {
                BepuCollision collision;
                if (contactToCollision.TryGetValue(contactPoint, out collision))
                {
                    while (collision.ContactEndedChannel.Balance < 0)
                    {
                        collision.ContactEndedChannel.Send(contactPoint);
                    }
                }
            }

            //contactsProfilingState.End("Contacts: {0}", currentFrameContacts.Count);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            NativeBepuSimulation?.Dispose();
        }

        /// <summary>
        /// Enables or disables the rendering of collider shapes
        /// </summary>
        public bool ColliderShapesRendering
        {
            set
            {
                processor.RenderColliderShapes(value);
            }
        }

        public RenderGroup ColliderShapesRenderGroup { get; set; } = RenderGroup.Group0;

        internal void SetStaticCollider(BepuStaticColliderComponent physicsComponent)
        {
            physicsComponent.Entity.Transform.WorldMatrix.Decompose(out _, out Quaternion orientation, out Vector3 position);

            physicsComponent.ColliderShape.CreateAndAddCollidableDescription(
                physicsComponent, this, out physicsComponent.NativeShapeTypeIndex, out var collidable);

            var desc = new StaticDescription(position.ToNumericsVector3(), orientation.ToBepuQuaternion(), collidable);

            if (physicsComponent.NativeStaticReference.Exists)
            {
                int handleId = physicsComponent.NativeStaticReference.Handle;
                NativeBepuSimulation.Statics.ApplyDescription(handleId, desc);
            }
            else
            {
                int staticHandleId = NativeBepuSimulation.Statics.Add(desc);
                var colliableRef = new CollidableReferenceKey(CollidableMobility.Static, staticHandleId);
                physicsComponent.NativeCollidableReference = colliableRef;

                collidableReferenceToComponent.Add(colliableRef, physicsComponent);

                physicsComponent.NativeStaticReference = new StaticReference(staticHandleId, NativeBepuSimulation.Statics);
            }
            //nativeBepuSimulation.AddCollisionObject(component.NativeCollisionObject, (BulletSharp.CollisionFilterGroups)group, (BulletSharp.CollisionFilterGroups)mask);
        }

        internal void RemoveStaticCollider(BepuStaticColliderComponent physicsComponent)
        {
            NativeBepuSimulation.Statics.Remove(physicsComponent.NativeCollidableReference.HandleId);
            collidableReferenceToComponent.Remove(physicsComponent.NativeCollidableReference);

            physicsComponent.ClearNativeStaticReference();
            // TODO remove from Shapes?
        }

        internal void SetRigidBody(BepuRigidbodyComponent physicsComponent, CollidableMobility collidableType)
        {
            physicsComponent.Entity.Transform.WorldMatrix.Decompose(out _, out Quaternion orientation, out Vector3 position);

            physicsComponent.ColliderShape.CreateAndAddCollidableDescription(
                physicsComponent, this, out physicsComponent.NativeShapeTypeIndex, out var collidable);

            BodyDescription desc;
            switch (collidableType)
            {
                case CollidableMobility.Dynamic:
                    {
                        BodyInertia bodyInertia;
                        if (physicsComponent.ColliderShape.InternalShape is IConvexShape convexShape)
                        {
                            convexShape.ComputeInertia(physicsComponent.Mass, out bodyInertia);
                        }
                        else
                        {
                            bodyInertia = new BodyInertia
                            {
                                InverseMass = 1 / physicsComponent.Mass
                            };
                        }
                        desc = BodyDescription.CreateDynamic(
                            new RigidPose(position.ToNumericsVector3(), orientation.ToBepuQuaternion()),
                            bodyInertia,
                            collidable,
                            new BodyActivityDescription(sleepThreshold: 0.01f)
                        );
                    }
                    break;
                case CollidableMobility.Kinematic:
                    {
                        desc = BodyDescription.CreateKinematic(
                            new RigidPose(position.ToNumericsVector3(), orientation.ToBepuQuaternion()),
                            collidable,
                            new BodyActivityDescription(sleepThreshold: 0.01f)
                        );
                    }
                    break;
                case CollidableMobility.Static:
                default:
                    Debug.Fail($"Invalid collidableType: {collidableType}");
                    desc = default;
                    break;
            }

            if (physicsComponent.NativeBodyReference.Exists)
            {
                physicsComponent.NativeBodyReference.ApplyDescription(desc);
            }
            else
            {
                int bodyHandleId = NativeBepuSimulation.Bodies.Add(desc);
                var colliableRef = new CollidableReferenceKey(collidableType, bodyHandleId);
                physicsComponent.NativeCollidableReference = colliableRef;

                Debug.Assert(!collidableReferenceToComponent.ContainsKey(colliableRef));
                collidableReferenceToComponent.Add(colliableRef, physicsComponent);

                physicsComponent.NativeBodyReference = NativeBepuSimulation.Bodies.GetBodyReference(bodyHandleId);
                //physicsComponent.NativeBodyDescription = desc;

                bodyHandleIdToComponent.Add(bodyHandleId, physicsComponent);
            }
        }

        private void RemoveNonStaticCollider(BepuPhysicsSkinnedComponentBase physicsComponent)
        {
            var bodyHandleId = physicsComponent.NativeCollidableReference.HandleId;
            NativeBepuSimulation.Bodies.Remove(bodyHandleId);
            bodyHandleIdToComponent.Remove(bodyHandleId);
            collidableReferenceToComponent.Remove(physicsComponent.NativeCollidableReference);

            physicsComponent.ClearNativeBodyReference();
        }

        internal void RemoveRigidBody(BepuRigidbodyComponent rigidBody)
        {
            //discreteDynamicsWorld.RemoveRigidBody(rigidBody.InternalRigidBody);

            RemoveNonStaticCollider(rigidBody);
        }

        //internal void AddCharacter(BepuCharacterComponent character, CollisionFilterGroupFlags group, CollisionFilterGroupFlags mask, CollidableMobility collidableType)
        //{
        //    RegisterNewBepuReference(character, collidableType);

        //    var collider = character.NativeCollisionObject;
        //    var action = character.KinematicCharacter;
        //    discreteDynamicsWorld.AddCollisionObject(collider, (BulletSharp.CollisionFilterGroups)group, (BulletSharp.CollisionFilterGroups)mask);
        //    discreteDynamicsWorld.AddAction(action);

        //    character.Simulation = this;
        //}

        //internal void RemoveCharacter(BepuCharacterComponent character)
        //{
        //    if (discreteDynamicsWorld == null) throw new Exception("Cannot perform this action when the physics engine is set to CollisionsOnly.");

        //    var collider = character.NativeCollisionObject;
        //    var action = character.KinematicCharacter;
        //    discreteDynamicsWorld.RemoveCollisionObject(collider);
        //    discreteDynamicsWorld.RemoveAction(action);

        //    character.Simulation = null;
        //}

        /// <summary>
        /// Creates the constraint.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="rigidBodyA">The rigid body a.</param>
        /// <param name="frameA">The frame a.</param>
        /// <param name="useReferenceFrameA">if set to <c>true</c> [use reference frame a].</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// Cannot perform this action when the physics engine is set to CollisionsOnly
        /// or
        /// Both RigidBodies must be valid
        /// or
        /// A Gear constraint always needs two rigidbodies to be created.
        /// </exception>
        public static BepuConstraint CreateConstraint(ConstraintTypes type, BepuRigidbodyComponent rigidBodyA, Matrix frameA, bool useReferenceFrameA = false)
        {
            return CreateConstraintInternal(type, rigidBodyA, frameA, useReferenceFrameA: useReferenceFrameA);
        }

        /// <summary>
        /// Creates the constraint.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="rigidBodyA">The rigid body a.</param>
        /// <param name="rigidBodyB">The rigid body b.</param>
        /// <param name="frameA">The frame a.</param>
        /// <param name="frameB">The frame b.</param>
        /// <param name="useReferenceFrameA">if set to <c>true</c> [use reference frame a].</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// Cannot perform this action when the physics engine is set to CollisionsOnly
        /// or
        /// Both RigidBodies must be valid
        /// </exception>
        public static BepuConstraint CreateConstraint(ConstraintTypes type, BepuRigidbodyComponent rigidBodyA, BepuRigidbodyComponent rigidBodyB, Matrix frameA, Matrix frameB, bool useReferenceFrameA = false)
        {
            if (rigidBodyA == null || rigidBodyB == null) throw new ArgumentException("Both RigidBodies must be valid.");
            return CreateConstraintInternal(type, rigidBodyA, frameA, rigidBodyB, frameB, useReferenceFrameA);
        }

        static BepuConstraint CreateConstraintInternal(ConstraintTypes type, BepuRigidbodyComponent rigidBodyA, Matrix frameA, BepuRigidbodyComponent rigidBodyB = null, Matrix frameB = default, bool useReferenceFrameA = false)
        {
            if (rigidBodyA == null) throw new ArgumentException($"{nameof(rigidBodyA)} must be valid.");
            if (rigidBodyB != null && rigidBodyB.Simulation != rigidBodyA.Simulation) throw new ArgumentException("Both RigidBodies must be on the same simulation.");

            BepuConstraint constraintBase;
            //var rbA = rigidBodyA.InternalRigidBody;
            //var rbB = rigidBodyB?.InternalRigidBody;
            switch (type)
            {
                case ConstraintTypes.Point2Point:
                    {
                        // Fixed distance between two colliders

                        var localOffsetA = frameA.TranslationVector.ToNumericsVector3();
                        var localOffsetB = (rigidBodyB == null ? frameA.TranslationVector : frameB.TranslationVector).ToNumericsVector3();

                        var distVector = rigidBodyB == null ? localOffsetA : (localOffsetB - localOffsetA);
                        var targetDistance = distVector.Length();
                        BepuPhysicsExtensions.Average(rigidBodyA.SpringSettings, rigidBodyB == null ? rigidBodyA.SpringSettings : rigidBodyB.SpringSettings, out var springSettings);
                        var distServo = new DistanceServo(
                            localOffsetA, localOffsetB,
                            targetDistance, springSettings);

                        var constraint = new Point2PointConstraint
                        {
                            InternalPoint2PointConstraint = distServo
                        };
                        constraintBase = constraint;

                        //constraint.InternalConstraint = constraint.InternalPoint2PointConstraint;
                        break;
                    }
                //case ConstraintTypes.Hinge:
                //    {
                //        //var constraint = new HingeConstraint
                //        //{
                //        //    InternalHingeConstraint =
                //        //        rigidBodyB == null ?
                //        //            new BulletSharp.HingeConstraint(rbA, frameA) :
                //        //            new BulletSharp.HingeConstraint(rbA, rbB, frameA, frameB, useReferenceFrameA),
                //        //};
                //        //constraintBase = constraint;

                //        //constraint.InternalConstraint = constraint.InternalHingeConstraint;
                //        break;
                //    }
                //case ConstraintTypes.Slider:
                //    {
                //        //var constraint = new SliderConstraint
                //        //{
                //        //    InternalSliderConstraint =
                //        //        rigidBodyB == null ?
                //        //            new BulletSharp.SliderConstraint(rbA, frameA, useReferenceFrameA) :
                //        //            new BulletSharp.SliderConstraint(rbA, rbB, frameA, frameB, useReferenceFrameA),
                //        //};
                //        //constraintBase = constraint;

                //        //constraint.InternalConstraint = constraint.InternalSliderConstraint;
                //        break;
                //    }
                //case ConstraintTypes.ConeTwist:
                //    {
                //        //var constraint = new ConeTwistConstraint
                //        //{
                //        //    InternalConeTwistConstraint =
                //        //        rigidBodyB == null ?
                //        //            new BulletSharp.ConeTwistConstraint(rbA, frameA) :
                //        //            new BulletSharp.ConeTwistConstraint(rbA, rbB, frameA, frameB),
                //        //};
                //        //constraintBase = constraint;

                //        //constraint.InternalConstraint = constraint.InternalConeTwistConstraint;
                //        break;
                //    }
                //case ConstraintTypes.Generic6DoF:
                //    {
                //        //var constraint = new Generic6DoFConstraint
                //        //{
                //        //    InternalGeneric6DofConstraint =
                //        //        rigidBodyB == null ?
                //        //            new BulletSharp.Generic6DofConstraint(rbA, frameA, useReferenceFrameA) :
                //        //            new BulletSharp.Generic6DofConstraint(rbA, rbB, frameA, frameB, useReferenceFrameA),
                //        //};
                //        //constraintBase = constraint;

                //        //constraint.InternalConstraint = constraint.InternalGeneric6DofConstraint;
                //        break;
                //    }
                //case ConstraintTypes.Generic6DoFSpring:
                //    {
                //        //var constraint = new Generic6DoFSpringConstraint
                //        //{
                //        //    InternalGeneric6DofSpringConstraint =
                //        //        rigidBodyB == null ?
                //        //            new BulletSharp.Generic6DofSpringConstraint(rbA, frameA, useReferenceFrameA) :
                //        //            new BulletSharp.Generic6DofSpringConstraint(rbA, rbB, frameA, frameB, useReferenceFrameA),
                //        //};
                //        //constraintBase = constraint;

                //        //constraint.InternalConstraint = constraint.InternalGeneric6DofConstraint = constraint.InternalGeneric6DofSpringConstraint;
                //        break;
                //    }
                //case ConstraintTypes.Gear:
                //    {
                //        //var constraint = new GearConstraint
                //        //{
                //        //    InternalGearConstraint =
                //        //        rigidBodyB == null ?
                //        //            throw new Exception("A Gear constraint always needs two rigidbodies to be created.") :
                //        //            new BulletSharp.GearConstraint(rbA, rbB, frameA.TranslationVector, frameB.TranslationVector),
                //        //};
                //        //constraintBase = constraint;

                //        //constraint.InternalConstraint = constraint.InternalGearConstraint;
                //        break;
                //    }
                default:
                    throw new ArgumentException(type.ToString());
            }

            constraintBase.RigidBodyA = rigidBodyA;
            if (rigidBodyB != null)
            {
                constraintBase.RigidBodyB = rigidBodyB;
                rigidBodyB.LinkedConstraints.Add(constraintBase);
            }
            rigidBodyA.LinkedConstraints.Add(constraintBase);

            return constraintBase;
        }


        /// <summary>
        /// Adds the constraint to the engine processing pipeline.
        /// </summary>
        /// <param name="constraint">The constraint.</param>
        /// <exception cref="InvalidOperationException">Cannot perform this action when the physics engine is set to CollisionsOnly</exception>
        public void AddConstraint(BepuConstraint constraint)
        {
            //if (discreteDynamicsWorld == null) throw new InvalidOperationException("Cannot perform this action when the physics engine is set to CollisionsOnly.");
            //
            //discreteDynamicsWorld.AddConstraint(constraint.InternalConstraint);

            constraint.Simulation = this;

            constraint.AddConstraintToSimulation();

        }

        ///// <summary>
        ///// Adds the constraint to the engine processing pipeline.
        ///// </summary>
        ///// <param name="constraint">The constraint.</param>
        ///// <param name="disableCollisionsBetweenLinkedBodies">if set to <c>true</c> [disable collisions between linked bodies].</param>
        ///// <exception cref="InvalidOperationException">Cannot perform this action when the physics engine is set to CollisionsOnly</exception>
        //public void AddConstraint(BepuConstraint constraint, bool disableCollisionsBetweenLinkedBodies)
        //{
        //    if (discreteDynamicsWorld == null) throw new InvalidOperationException("Cannot perform this action when the physics engine is set to CollisionsOnly.");

        //    discreteDynamicsWorld.AddConstraint(constraint.InternalConstraint, disableCollisionsBetweenLinkedBodies);
        //    constraint.Simulation = this;
        //}

        /// <summary>
        /// Removes the constraint from the engine processing pipeline.
        /// </summary>
        /// <param name="constraint">The constraint.</param>
        /// <exception cref="InvalidOperationException">Cannot perform this action when the physics engine is set to CollisionsOnly</exception>
        public void RemoveConstraint(BepuConstraint constraint)
        {
            //if (discreteDynamicsWorld == null) throw new InvalidOperationException("Cannot perform this action when the physics engine is set to CollisionsOnly.");
            //
            //discreteDynamicsWorld.RemoveConstraint(constraint.InternalConstraint);

            NativeBepuSimulation.Solver.Remove(constraint.InternalConstraintHandleId);
            constraint.InternalConstraintHandleId = ConstraintHandleId.NotSet;

            constraint.Simulation = null;
        }

        /// <summary>
        /// Raycasts and returns the closest hit.
        /// </summary>
        /// <param name="from">From world position.</param>
        /// <param name="to">To world position.</param>
        /// <param name="filterGroup">The collision group of this raycast</param>
        /// <param name="filterFlags">The collision group that this raycast can collide with</param>
        /// <returns>The closest hit result.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BepuHitResult Raycast(Vector3 from, Vector3 to, CollisionFilterGroups filterGroup = DefaultGroup, CollisionFilterGroupFlags filterFlags = DefaultFlags)
        {
            Raycast(from, to, out var result, filterGroup, filterFlags);
            return result;
        }

        /// <summary>
        /// Raycasts, returns true when it hit something.
        /// </summary>
        /// <param name="from">From world position.</param>
        /// <param name="to">To world position.</param>
        /// <param name="result">Raycast info</param>
        /// <param name="filterGroup">The collision group of this raycast</param>
        /// <param name="filterFlags">The collision group that this raycast can collide with</param>
        /// <returns>True if it hit something</returns>
        public bool Raycast(Vector3 from, Vector3 to, out BepuHitResult result, CollisionFilterGroups filterGroup = DefaultGroup, CollisionFilterGroupFlags filterFlags = DefaultFlags)
        {
            var rayVector = to - from;
            Vector3.Normalize(ref rayVector, out var rayDir);
            var rayDirection = rayDir.ToNumericsVector3();
            float maxRaycastLength = rayVector.Length();
            var rayCastHandler = new BepuRaycastClosestHitHandler(this, maxRaycastLength, filterGroup, filterFlags);
            NativeBepuSimulation.RayCast(from.ToNumericsVector3(), rayDirection, maximumT: maxRaycastLength, ref rayCastHandler);
            result = rayCastHandler.Result;
            return result.Succeeded;
        }

        /// <summary>
        /// Raycasts penetrating any shape the ray encounters.
        /// Filtering by CollisionGroup.
        /// </summary>
        /// <param name="from">From world position.</param>
        /// <param name="to">To world position.</param>
        /// <param name="resultsOutput">The list to fill with results.</param>
        /// <param name="filterGroup">The collision group of this raycast</param>
        /// <param name="filterFlags">The collision group that this raycast can collide with</param>
        public void RaycastPenetrating<TList>(Vector3 from, Vector3 to, TList resultsOutput, CollisionFilterGroups filterGroup = DefaultGroup, CollisionFilterGroupFlags filterFlags = DefaultFlags)
            where TList : IList<BepuHitResult>
        {
            var rayVector = to - from;
            Vector3.Normalize(ref rayVector, out var rayDir);
            var rayDirection = rayDir.ToNumericsVector3();
            float maxRaycastLength = rayVector.Length();
            var rayCastHandler = new BepuRaycastAllHitsHandler<TList>(this, resultsOutput, filterGroup, filterFlags);
            NativeBepuSimulation.RayCast(from.ToNumericsVector3(), rayDirection, maximumT: maxRaycastLength, ref rayCastHandler);
        }

        /// <summary>
        /// Performs a sweep test using a collider shape and returns the closest hit.
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <param name="fromPosition">From world position.</param>
        /// <param name="fromOrientation">From world orientation.</param>
        /// <param name="toPosition">To world position.</param>
        /// <param name="velocity">The linear velocity of the shape.</param>
        /// <param name="filterGroup">The collision group of this shape sweep</param>
        /// <param name="filterFlags">The collision group that this shape sweep can collide with</param>
        public BepuHitResult ShapeSweep<TShape>(
            TShape shape,
            in Vector3 fromPosition, in Quaternion fromOrientation,
            in Vector3 toPosition, in Vector3 velocity,
            CollisionFilterGroups filterGroup = DefaultGroup, CollisionFilterGroupFlags filterFlags = DefaultFlags)
            where TShape : IConvexShape
        {
            var rigidPose = new RigidPose
            {
                Position = fromPosition.ToNumericsVector3(),
                Orientation = fromOrientation.ToBepuQuaternion()
            };

            var posVector = toPosition - fromPosition;
            float maxTranslationLength = posVector.Length();
            var bodyVelocity = new BodyVelocity
            {
                Linear = velocity.ToNumericsVector3(),
            };
            var hitHandler = new BepuSweepClosestHitHandler(this, maxTranslationLength, filterGroup, filterFlags);
            NativeBepuSimulation.Sweep(shape, rigidPose, bodyVelocity, maximumT: maxTranslationLength, BufferPool, ref hitHandler);

            return hitHandler.Result;
        }

        /// <summary>
        /// Performs a sweep test using a collider shape and never stops until "to"
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <param name="fromPosition">From world position.</param>
        /// <param name="fromOrientation">From world orientation.</param>
        /// <param name="toPosition">To world position.</param>
        /// <param name="velocity">The linear velocity of the shape.</param>
        /// <param name="resultsOutput">The list to fill with results.</param>
        /// <param name="filterGroup">The collision group of this shape sweep</param>
        /// <param name="filterFlags">The collision group that this shape sweep can collide with</param>
        public void ShapeSweepPenetrating<TShape, TList>(
            TShape shape,
            in Vector3 fromPosition, in Quaternion fromOrientation,
            in Vector3 toPosition, in Vector3 velocity,
            TList resultsOutput,
            CollisionFilterGroups filterGroup = DefaultGroup, CollisionFilterGroupFlags filterFlags = DefaultFlags)
            where TShape : IConvexShape
            where TList : IList<BepuHitResult>
        {
            var rigidPose = new RigidPose
            {
                Position = fromPosition.ToNumericsVector3(),
                Orientation = fromOrientation.ToBepuQuaternion()
            };

            var posVector = toPosition - fromPosition;
            float maxTranslationLength = posVector.Length();
            var bodyVelocity = new BodyVelocity
            {
                Linear = velocity.ToNumericsVector3(),
            };
            var hitHandler = new BepuSweepAllHitHandler<TList>(this, resultsOutput, filterGroup, filterFlags);
            NativeBepuSimulation.Sweep(shape, rigidPose, bodyVelocity, maximumT: maxTranslationLength, BufferPool, ref hitHandler);
        }

        /// <summary>
        /// Gets or sets the gravity.
        /// </summary>
        /// <value>
        /// The gravity.
        /// </value>
        public Vector3 Gravity { get; set; } = new Vector3(0, -9.8f, 0);

        /// <summary>
        /// The maximum number of steps that the Simulation is allowed to take each tick.
        /// If the engine is running slow (large deltaTime), then you must increase the number of maxSubSteps to compensate for this, otherwise your simulation is “losing” time.
        /// It's important that frame DeltaTime is always less than MaxSubSteps*FixedTimeStep, otherwise you are losing time.
        /// </summary>
        public int MaxSubSteps { get; set; }

        /// <summary>
        /// By decreasing the size of fixedTimeStep, you are increasing the “resolution” of the simulation.
        /// Default is 1.0f / 60.0f or 60fps
        /// </summary>
        public float FixedTimeStep { get; set; }

        public AngularIntegrationMode AngularIntegrationMode { get; set; }

        //public void ClearForces()
        //{
        //    if (discreteDynamicsWorld == null) throw new InvalidOperationException("Cannot perform this action when the physics engine is set to CollisionsOnly.");
        //    discreteDynamicsWorld.ClearForces();
        //}

        //public bool SpeculativeContactRestitution
        //{
        //    get
        //    {
        //        if (discreteDynamicsWorld == null) throw new InvalidOperationException("Cannot perform this action when the physics engine is set to CollisionsOnly.");
        //        return discreteDynamicsWorld.ApplySpeculativeContactRestitution;
        //    }
        //    set
        //    {
        //        if (discreteDynamicsWorld == null) throw new InvalidOperationException("Cannot perform this action when the physics engine is set to CollisionsOnly.");
        //        discreteDynamicsWorld.ApplySpeculativeContactRestitution = value;
        //    }
        //}

        public class SimulationArgs : EventArgs
        {
            public float DeltaTime;
        }

        /// <summary>
        /// Called right before the physics simulation.
        /// This event might not be fired by the main thread.
        /// </summary>
        public event EventHandler<SimulationArgs> SimulationBegin;

        protected virtual void OnSimulationBegin(SimulationArgs e)
        {
            var handler = SimulationBegin;
            handler?.Invoke(this, e);
        }

        internal int UpdatedRigidbodies;

        private readonly SimulationArgs simulationArgs = new SimulationArgs();

        internal ProfilingState SimulationProfiler;

        private float accumulatedSimulationTime = 0;

        internal void Simulate(float deltaTime)
        {
            if (NativeBepuSimulation == null) return;

            simulationArgs.DeltaTime = deltaTime;

            UpdatedRigidbodies = 0;

            OnSimulationBegin(simulationArgs);

            SimulationProfiler = Profiler.Begin(PhysicsProfilingKeys.SimulationProfilingKey);

            accumulatedSimulationTime += deltaTime;
            int estimatedSteps = (int)(accumulatedSimulationTime / FixedTimeStep);
            int steps = MathHelper.Clamp(estimatedSteps, 0, MaxSubSteps);
            for (int i = 0; i < steps; i++)
            {
                NativeBepuSimulation.Timestep(FixedTimeStep);
            }
            accumulatedSimulationTime -= estimatedSteps * FixedTimeStep;    // Multiply against estimatedSteps since accumulatedSimulationTime might be capped by MaxSubSteps

            SimulationProfiler.End("Alive rigidbodies: {0}", UpdatedRigidbodies);

            OnSimulationEnd(simulationArgs);
        }

        /// <summary>
        /// Called right after the physics simulation.
        /// This event might not be fired by the main thread.
        /// </summary>
        public event EventHandler<SimulationArgs> SimulationEnd;

        protected virtual void OnSimulationEnd(SimulationArgs e)
        {
            var handler = SimulationEnd;
            handler?.Invoke(this, e);
        }

        internal void UpdateRigidbodyPosition(int handleId)
        {
            var physComp = bodyHandleIdToComponent[handleId];
            if (physComp is BepuRigidbodyComponent rigidBodyComp)
            {
                var worldTransform = rigidBodyComp.PhysicsWorldTransform;
                rigidBodyComp.RigidBodySetWorldTransform(ref worldTransform);
                if (physComp.DebugEntity != null)
                {
                    Vector3 pos;
                    Quaternion rot;
                    worldTransform.Decompose(out _, out rot, out pos);
                    physComp.DebugEntity.Transform.Position = pos;
                    physComp.DebugEntity.Transform.Rotation = rot;
                }
            }
        }

        private readonly FastList<BepuContactPoint> newContacts = new FastList<BepuContactPoint>();
        private readonly FastList<BepuContactPoint> updatedContacts = new FastList<BepuContactPoint>();
        private readonly FastList<BepuContactPoint> removedContacts = new FastList<BepuContactPoint>();

        private readonly Queue<BepuCollision> collisionsPool = new Queue<BepuCollision>();

        internal void BeginContactTesting()
        {
            // Remove previous frame removed collisions
            foreach (var collision in removedCollisionsCache)
            {
                collision.Destroy();
                collisionsPool.Enqueue(collision);
            }

            // Clean caches
            newCollisionsCache.Clear();
            removedCollisionsCache.Clear();
            newContactsFastCache.Clear();
            updatedContactsCache.Clear();
            removedContactsCache.Clear();

            // Swap the lists
            var previous = currentFrameContacts;
            currentFrameContacts = previousFrameContacts;
            currentFrameContacts.Clear();
            previousFrameContacts = previous;
        }

        private void ContactRemoval(BepuContactPoint contact, BepuPhysicsComponent component0, BepuPhysicsComponent component1)
        {
            BepuCollision existingPair = null;
            foreach (var x in component0.Collisions)
            {
                if (x.InternalEquals(component0, component1))
                {
                    existingPair = x;
                    break;
                }
            }
            if (existingPair == null)
            {
#if DEBUG
                //should not happen?
                Log.Warning("Pair not present.");
#endif
                return;
            }

            if (existingPair.Contacts.Contains(contact))
            {
                existingPair.Contacts.Remove(contact);
                removedContactsCache.Add(contact);

                contactToCollision.Remove(contact);

                if (existingPair.Contacts.Count == 0)
                {
                    component0.Collisions.Remove(existingPair);
                    component1.Collisions.Remove(existingPair);
                    removedCollisionsCache.Add(existingPair);
                }
            }
            else
            {
#if DEBUG
                //should not happen?
                Log.Warning("Contact not in pair.");
#endif
            }
        }

        internal void EndContactTesting()
        {
            newContacts.Clear(true);
            updatedContacts.Clear(true);
            removedContacts.Clear(true);

            foreach (var currentFrameContact in currentFrameContacts)
            {
                if (!previousFrameContacts.Contains(currentFrameContact))
                {
                    newContacts.Add(currentFrameContact);
                }
                else
                {
                    updatedContacts.Add(currentFrameContact);
                }
            }

            foreach (var previousFrameContact in previousFrameContacts)
            {
                if (!currentFrameContacts.Contains(previousFrameContact))
                {
                    removedContacts.Add(previousFrameContact);
                }
            }

            foreach (var contact in newContacts)
            {
                var component0 = contact.ColliderA;
                var component1 = contact.ColliderB;

                BepuCollision existingPair = null;
                foreach (var x in component0.Collisions)
                {
                    if (x.InternalEquals(component0, component1))
                    {
                        existingPair = x;
                        break;
                    }
                }
                if (existingPair != null)
                {
                    if (existingPair.Contacts.Contains(contact))
                    {
#if DEBUG
                        //should not happen?
                        Log.Warning("Contact already added.");
#endif
                        continue;
                    }

                    existingPair.Contacts.Add(contact);
                }
                else
                {
                    var newPair = collisionsPool.Count == 0 ? new BepuCollision() : collisionsPool.Dequeue();
                    newPair.Initialize(component0, component1);
                    newPair.Contacts.Add(contact);
                    component0.Collisions.Add(newPair);
                    component1.Collisions.Add(newPair);

                    contactToCollision.Add(contact, newPair);

                    newCollisionsCache.Add(newPair);
                    newContactsFastCache.Add(contact);
                }
            }

            foreach (var contact in updatedContacts)
            {
                var component0 = contact.ColliderA;
                var component1 = contact.ColliderB;

                BepuCollision existingPair = null;
                foreach (var x in component0.Collisions)
                {
                    if (x.InternalEquals(component0, component1))
                    {
                        existingPair = x;
                        break;
                    }
                }
                if (existingPair != null)
                {
                    if (existingPair.Contacts.Contains(contact))
                    {
                        //update data values (since comparison is only at pointer level internally)
                        existingPair.Contacts.Remove(contact);
                        existingPair.Contacts.Add(contact);
                        updatedContactsCache.Add(contact);
                    }
                    else
                    {
#if DEBUG
                        //should not happen?
                        Log.Warning("Contact not in pair.");
#endif
                    }
                }
                else
                {
#if DEBUG
                    //should not happen?
                    Log.Warning("Pair not present.");
#endif
                }
            }

            foreach (var contact in removedContacts)
            {
                var component0 = contact.ColliderA;
                var component1 = contact.ColliderB;

                ContactRemoval(contact, component0, component1);
            }
        }

        private HashSet<BepuContactPoint> currentFrameContacts = new HashSet<BepuContactPoint>();
        private HashSet<BepuContactPoint> previousFrameContacts = new HashSet<BepuContactPoint>();

        struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
        {
            private readonly BepuSimulation xenkoSimulation;

            public NarrowPhaseCallbacks(BepuSimulation xenkoSimulation)
            {
                this.xenkoSimulation = xenkoSimulation;
            }

            /// <summary>
            /// Performs any required initialization logic after the Simulation instance has been constructed.
            /// </summary>
            /// <param name="simulation">Simulation that owns these callbacks.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Initialize(BepuPhysics.Simulation simulation)
            {
                // Do nothing
            }

            /// <summary>
            /// Releases any resources held by the callbacks. Called by the owning narrow phase when it is being disposed.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                // Do nothing
            }

            /// <summary>
            /// Chooses whether to allow contact generation to proceed for two overlapping collidables.
            /// </summary>
            /// <param name="workerIndex">Index of the worker that identified the overlap.</param>
            /// <param name="a">Reference to the first collidable in the pair.</param>
            /// <param name="b">Reference to the second collidable in the pair.</param>
            /// <returns>True if collision detection should proceed, false otherwise.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b)
            {
                if (a.Mobility == CollidableMobility.Static && b.Mobility == CollidableMobility.Static)
                {
                    return false;
                }
                if (xenkoSimulation.collidableReferenceToComponent.TryGetValue(a, out var physCompA)
                    && xenkoSimulation.collidableReferenceToComponent.TryGetValue(b, out var physCompB))
                {
                    if (!physCompA.Enabled || !physCompB.Enabled)
                    {
                        return false;
                    }

                    bool canCollide = (physCompA.CanCollideWith & (CollisionFilterGroupFlags)physCompB.CollisionGroup) != 0;
                    //&& a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic);
                    return canCollide;
                }

                Debug.Fail($"CollidableReference not registered: {(physCompA == null ? a.ToString() : b.ToString())}");
                return false;
                //return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
            }

            /// <summary>
            /// Chooses whether to allow contact generation to proceed for the children of two overlapping collidables in a compound-including pair.
            /// </summary>
            /// <param name="pair">Parent pair of the two child collidables.</param>
            /// <param name="childIndexA">Index of the child of collidable A in the pair. If collidable A is not compound, then this is always 0.</param>
            /// <param name="childIndexB">Index of the child of collidable B in the pair. If collidable B is not compound, then this is always 0.</param>
            /// <returns>True if collision detection should proceed, false otherwise.</returns>
            /// <remarks>This is called for each sub-overlap in a collidable pair involving compound collidables. If neither collidable in a pair is compound, this will not be called.
            /// For compound-including pairs, if the earlier call to AllowContactGeneration returns false for owning pair, this will not be called. Note that it is possible
            /// for this function to be called twice for the same subpair if the pair has continuous collision detection enabled;
            /// the CCD sweep test that runs before the contact generation test also asks before performing child pair tests.</remarks>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
            {
                return true;
            }

            /// <summary>
            /// Provides a notification that a manifold has been created for a pair. Offers an opportunity to change the manifold's details.
            /// </summary>
            /// <param name="workerIndex">Index of the worker thread that created this manifold.</param>
            /// <param name="pair">Pair of collidables that the manifold was detected between.</param>
            /// <param name="manifold">Set of contacts detected between the collidables.</param>
            /// <param name="pairMaterial">Material properties of the manifold.</param>
            /// <returns>True if a constraint should be created for the manifold, false otherwise.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial)
                where TManifold : struct, IContactManifold<TManifold>
            {
                ref readonly var collidableReferenceToComponent = ref xenkoSimulation.collidableReferenceToComponent;
                if (!collidableReferenceToComponent.TryGetValue(pair.A, out var physCompA)
                    || !collidableReferenceToComponent.TryGetValue(pair.B, out var physCompB))
                {
                    pairMaterial = default;
                    return false;
                }

                if (pair.B.Mobility != CollidableMobility.Static)
                {
                    // If two bodies collide, just average the friction.
                    pairMaterial.FrictionCoefficient = (physCompA.Friction + physCompB.Friction) * 0.5f;
                    // TODO: Should these be averaged?
                    pairMaterial.MaximumRecoveryVelocity = (physCompA.MaximumRecoveryVelocity + physCompB.MaximumRecoveryVelocity) * 0.5f;
                    BepuPhysicsExtensions.Average(physCompA.SpringSettings, physCompB.SpringSettings, out pairMaterial.SpringSettings);
                }
                else
                {
                    pairMaterial.FrictionCoefficient = physCompA.Friction;
                    pairMaterial.MaximumRecoveryVelocity = physCompA.MaximumRecoveryVelocity;
                    pairMaterial.SpringSettings = physCompA.SpringSettings;
                }

                if (manifold.Count <= 0)
                {
                    // Occurs for constraints?
                    return false;
                }
                //Debug.Assert(manifold.Count > 0);
                // Register contacts
                // Only bother with the first contact point.
                manifold.GetContact(0, out var offset, out var normal, out float depth, out _);
                Vector3 positionOnA;
                if (physCompA is BepuPhysicsSkinnedComponentBase nonStaticPhysCompA)
                {
                    positionOnA = (nonStaticPhysCompA.NativeBodyReference.Pose.Position + offset).ToXenkoVector3();
                }
                else if (physCompA is BepuStaticColliderComponent staticPhysCompA)
                {
                    positionOnA = (staticPhysCompA.NativeStaticReference.Pose.Position + offset).ToXenkoVector3();
                }
                else
                {
                    positionOnA = physCompA.Entity.Transform.Position;
                }
                var xenkoNormal = normal.ToXenkoVector3();
                var offsetFromContactAToContactB = xenkoNormal * depth;
                var positionOnB = positionOnA + offsetFromContactAToContactB;

                var contactPoint = new BepuContactPoint
                {
                    ColliderA = physCompA,
                    ColliderB = physCompB,
                    Distance = depth,
                    Normal = xenkoNormal,
                    PositionOnA = positionOnA,
                    PositionOnB = positionOnB,
                };
                xenkoSimulation.currentFrameContacts.Add(contactPoint);

                if ((physCompA is BepuPhysicsTriggerComponentBase triggerA && triggerA.IsTrigger)
                    || (physCompB is BepuPhysicsTriggerComponentBase triggerB && triggerB.IsTrigger))
                {
                    return false;
                }

                return true;
            }

            /// <summary>
            /// Provides a notification that a manifold has been created between the children of two collidables in a compound-including pair.
            /// Offers an opportunity to change the manifold's details.
            /// </summary>
            /// <param name="workerIndex">Index of the worker thread that created this manifold.</param>
            /// <param name="pair">Pair of collidables that the manifold was detected between.</param>
            /// <param name="childIndexA">Index of the child of collidable A in the pair. If collidable A is not compound, then this is always 0.</param>
            /// <param name="childIndexB">Index of the child of collidable B in the pair. If collidable B is not compound, then this is always 0.</param>
            /// <param name="manifold">Set of contacts detected between the collidables.</param>
            /// <returns>True if this manifold should be considered for constraint generation, false otherwise.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
            {
                return true;
            }
        }

        struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
        {
            private readonly BepuSimulation xenkoSimulation;
            private BepuPhysics.Simulation nativeBepuSimulation;

            private System.Numerics.Vector3 defaultGravityDt;
            private float currentDt;

            public AngularIntegrationMode AngularIntegrationMode => xenkoSimulation.AngularIntegrationMode;

            public PoseIntegratorCallbacks(BepuSimulation xenkoSimulation) : this()
            {
                this.xenkoSimulation = xenkoSimulation;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void PrepareForIntegration(float dt)
            {
                nativeBepuSimulation = xenkoSimulation.NativeBepuSimulation;
                // Cache the default gravity calculation.
                defaultGravityDt = xenkoSimulation.Gravity.ToNumericsVector3() * dt;
                currentDt = dt;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void IntegrateVelocity(int bodyIndex, in RigidPose pose, in BodyInertia localInertia, int workerIndex, ref BodyVelocity velocity)
            {
                if (localInertia.InverseMass <= 0)
                {
                    return;
                }

                int bodyHandleId = nativeBepuSimulation.Bodies.ActiveSet.IndexToHandle[bodyIndex];
                //var physComp = xenkoSimulation.bodyHandleIdToComponent[bodyHandleId];
                bool compFound = xenkoSimulation.bodyHandleIdToComponent.TryGetValue(bodyHandleId, out var physComp);
                Debug.Assert(compFound, "BodyHandleId to Component not found.");
                var gravityDt = defaultGravityDt;

                float linearDampingDt = 1;
                float angularDampingDt = 1;

                if (physComp is BepuRigidbodyComponent rigidBodyComp)
                {
                    if (rigidBodyComp.OverrideGravity)
                    {
                        gravityDt = rigidBodyComp.InternalGravity * currentDt;
                    }

                    if (rigidBodyComp.LinearDamping != 0)
                    {
                        //linearDampingDt = MathF.Pow(MathHelper.Clamp(1 - rigidBodyComp.LinearDamping, 0, 1), currentDt);
                        linearDampingDt = (float)Math.Pow(MathHelper.Clamp(1 - rigidBodyComp.LinearDamping, 0, 1), currentDt);
                    }
                    if (rigidBodyComp.AngularDamping != 0)
                    {
                        //angularDampingDt = MathF.Pow(MathHelper.Clamp(1 - rigidBodyComp.AngularDamping, 0, 1), currentDt);
                        angularDampingDt = (float)Math.Pow(MathHelper.Clamp(1 - rigidBodyComp.AngularDamping, 0, 1), currentDt);
                    }
                }
                //else if (physComp is BepuCharacterComponent charComp)
                //{
                //    gravityDt = charComp.InternalGravity * currentDt;
                //}

                velocity.Linear = (velocity.Linear + gravityDt) * linearDampingDt;
                velocity.Angular = velocity.Angular * angularDampingDt;
            }
        }

        internal unsafe void ContactTest(BepuPhysicsComponent component)
        {
            //currentFrameContacts.CollisionFilterMask = (int)component.CanCollideWith;
            //currentFrameContacts.CollisionFilterGroup = (int)component.CollisionGroup;
            //NativeBepuSimulation.ContactTest(component.NativeCollisionObject, currentFrameContacts);
        }

        private readonly FastList<BepuContactPoint> currentToRemove = new FastList<BepuContactPoint>();

        internal void CleanContacts(BepuPhysicsComponent component)
        {
            currentToRemove.Clear(true);

            foreach (var currentFrameContact in currentFrameContacts)
            {
                var component0 = currentFrameContact.ColliderA;
                var component1 = currentFrameContact.ColliderB;
                if (component == component0 || component == component1)
                {
                    currentToRemove.Add(currentFrameContact);
                    ContactRemoval(currentFrameContact, component0, component1);
                }
            }

            foreach (var contactPoint in currentToRemove)
            {
                currentFrameContacts.Remove(contactPoint);
            }
        }

        private struct BepuNativeSweepResult
        {
            public float T;
            public float MaximumT;
            public System.Numerics.Vector3 Normal;
            public System.Numerics.Vector3 Location;
            public CollidableReference Collidable;

            public BepuHitResult ToHitResult(BepuSimulation simulation)
            {
                var collider = simulation.collidableReferenceToComponent[Collidable];
                var result = new BepuHitResult
                {
                    Succeeded = true,
                    Collider = collider,
                    HitFraction = T / MaximumT,
                    Normal = Normal.ToXenkoVector3(),
                    Point = Location.ToXenkoVector3()
                };
                return result;
            }
        }

        private struct BepuSweepAllHitHandler<TList> : ISweepHitHandler where TList : IList<BepuHitResult>
        {
            private readonly BepuSimulation xenkoSimulation;
            private readonly CollisionFilterGroups filterGroup;
            private readonly CollisionFilterGroupFlags filterMask;

            public readonly TList HitResults;

            /// <param name="filterGroup">The collision group of this raycast</param>
            /// <param name="filterFlags">The collision group that this raycast can collide with</param>
            public BepuSweepAllHitHandler(
                BepuSimulation xenkoSimulation,
                TList hitResults,
                CollisionFilterGroups filterGroup = DefaultGroup,
                CollisionFilterGroupFlags filterMask = DefaultFlags)
            {
                this.xenkoSimulation = xenkoSimulation;
                this.filterGroup = filterGroup;
                this.filterMask = filterMask;

                HitResults = hitResults;
            }

            /// <summary>
            /// Checks whether to run a detailed sweep test against a target collidable.
            /// </summary>
            /// <param name="collidable">Collidable to check.</param>
            /// <returns>True if the sweep test should be attempted, false otherwise.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowTest(CollidableReference collidable)
            {
                if (xenkoSimulation.collidableReferenceToComponent.TryGetValue(collidable, out var physComp))
                {
                    if (physComp.Enabled && (filterMask & (CollisionFilterGroupFlags)physComp.CollisionGroup) != 0)
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Checks whether to run a detailed sweep test against a target collidable's child.
            /// </summary>
            /// <param name="collidable">Collidable to check.</param>
            /// <param name="child">Index of the child in the collidable to check.</param>
            /// <returns>True if the sweep test should be attempted, false otherwise.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowTest(CollidableReference collidable, int child)
            {
                return true;
            }

            /// <summary>
            /// Called when a sweep test detects a hit with nonzero T value.
            /// </summary>
            /// <param name="maximumT">Reference to maximumT passed to the traversal.</param>
            /// <param name="t">Impact time of the sweep test.</param>
            /// <param name="hitLocation">Location of the first hit detected by the sweep.</param>
            /// <param name="hitNormal">Surface normal at the hit location.</param>
            /// <param name="collidable">Collidable hit by the traversal.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnHit(ref float maximumT, float t, in System.Numerics.Vector3 hitLocation, in System.Numerics.Vector3 hitNormal, CollidableReference collidable)
            {
                var nativeResult = new BepuNativeSweepResult
                {
                    T = t,
                    MaximumT = maximumT,
                    Normal = hitNormal,
                    Location = hitLocation,
                    Collidable = collidable
                };
                HitResults.Add(nativeResult.ToHitResult(xenkoSimulation));
            }

            /// <summary>
            /// Called when a sweep test detects a hit at T = 0, meaning that no location or normal can be computed.
            /// </summary>
            /// <param name="maximumT">Reference to maximumT passed to the traversal.</param>
            /// <param name="collidable">Collidable hit by the traversal.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
            {
                var nativeResult = new BepuNativeSweepResult
                {
                    //T = t,
                    MaximumT = maximumT,
                    //Normal = hitNormal,
                    //Location = hitLocation,
                    Collidable = collidable
                };
                HitResults.Add(nativeResult.ToHitResult(xenkoSimulation));
            }
        }

        private struct BepuSweepClosestHitHandler : ISweepHitHandler
        {
            private readonly BepuSimulation xenkoSimulation;
            private readonly CollisionFilterGroups filterGroup;
            private readonly CollisionFilterGroupFlags filterMask;
            private readonly float maxTranslationLength;

            private float currentClosestT;
            private BepuNativeSweepResult closestHit;

            public BepuHitResult Result => currentClosestT == float.PositiveInfinity ? BepuHitResult.NoHit : closestHit.ToHitResult(xenkoSimulation);

            /// <param name="filterGroup">The collision group of this raycast</param>
            /// <param name="filterFlags">The collision group that this raycast can collide with</param>
            public BepuSweepClosestHitHandler(
                BepuSimulation xenkoSimulation,
                float maxTranslationLength,
                CollisionFilterGroups filterGroup = DefaultGroup,
                CollisionFilterGroupFlags filterMask = DefaultFlags) : this()
            {
                this.xenkoSimulation = xenkoSimulation;
                this.filterGroup = filterGroup;
                this.filterMask = filterMask;
                this.maxTranslationLength = maxTranslationLength;
                currentClosestT = float.PositiveInfinity;
            }

            /// <summary>
            /// Checks whether to run a detailed sweep test against a target collidable.
            /// </summary>
            /// <param name="collidable">Collidable to check.</param>
            /// <returns>True if the sweep test should be attempted, false otherwise.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowTest(CollidableReference collidable)
            {
                if (xenkoSimulation.collidableReferenceToComponent.TryGetValue(collidable, out var physComp))
                {
                    if (physComp.Enabled && (filterMask & (CollisionFilterGroupFlags)physComp.CollisionGroup) != 0)
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Checks whether to run a detailed sweep test against a target collidable's child.
            /// </summary>
            /// <param name="collidable">Collidable to check.</param>
            /// <param name="child">Index of the child in the collidable to check.</param>
            /// <returns>True if the sweep test should be attempted, false otherwise.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowTest(CollidableReference collidable, int child)
            {
                return true;
            }

            /// <summary>
            /// Called when a sweep test detects a hit with nonzero T value.
            /// </summary>
            /// <param name="maximumT">Reference to maximumT passed to the traversal.</param>
            /// <param name="t">Impact time of the sweep test.</param>
            /// <param name="hitLocation">Location of the first hit detected by the sweep.</param>
            /// <param name="hitNormal">Surface normal at the hit location.</param>
            /// <param name="collidable">Collidable hit by the traversal.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnHit(ref float maximumT, float t, in System.Numerics.Vector3 hitLocation, in System.Numerics.Vector3 hitNormal, CollidableReference collidable)
            {
                // We can reduce maximumT so that we don't need to count collisions that occur greater than the new maximumT.
                if (t < maximumT)
                {
                    maximumT = t;
                }
                if (t < currentClosestT)
                {
                    currentClosestT = t;
                    closestHit = new BepuNativeSweepResult
                    {
                        T = t,
                        MaximumT = maxTranslationLength,
                        Normal = hitNormal,
                        Location = hitLocation,
                        Collidable = collidable
                    };
                }
            }

            /// <summary>
            /// Called when a sweep test detects a hit at T = 0, meaning that no location or normal can be computed.
            /// </summary>
            /// <param name="maximumT">Reference to maximumT passed to the traversal.</param>
            /// <param name="collidable">Collidable hit by the traversal.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
            {
                maximumT = 0;
                currentClosestT = 0;
                closestHit = new BepuNativeSweepResult
                {
                    //T = t,
                    MaximumT = maxTranslationLength,
                    //Normal = hitNormal,
                    //Location = hitLocation,
                    Collidable = collidable
                };
            }
        }

        private struct BepuNativeRaycastResult
        {
            public RayData RayData;
            public float T;
            public float MaximumT;
            public System.Numerics.Vector3 Normal;
            public CollidableReference Collidable;
            public int ChildIndex;

            public BepuHitResult ToHitResult(BepuSimulation simulation)
            {
                var result = new BepuHitResult
                {
                    Succeeded = true,
                    Collider = simulation.collidableReferenceToComponent[Collidable],
                    HitFraction = T / MaximumT,
                    Normal = Normal.ToXenkoVector3(),
                    Point = (RayData.Origin + (RayData.Direction * T)).ToXenkoVector3()
                };
                return result;
            }
        }

        private struct BepuRaycastAllHitsHandler<TList> : IRayHitHandler where TList : IList<BepuHitResult>
        {
            private readonly BepuSimulation xenkoSimulation;
            private readonly CollisionFilterGroups filterGroup;
            private readonly CollisionFilterGroupFlags filterMask;

            public readonly TList HitResults;

            /// <param name="filterGroup">The collision group of this raycast</param>
            /// <param name="filterFlags">The collision group that this raycast can collide with</param>
            public BepuRaycastAllHitsHandler(
                BepuSimulation xenkoSimulation,
                TList hitResults,
                CollisionFilterGroups filterGroup = DefaultGroup,
                CollisionFilterGroupFlags filterMask = DefaultFlags)
            {
                this.xenkoSimulation = xenkoSimulation;
                this.filterGroup = filterGroup;
                this.filterMask = filterMask;

                HitResults = hitResults;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowTest(CollidableReference collidable)
            {
                if (xenkoSimulation.collidableReferenceToComponent.TryGetValue(collidable, out var physComp))
                {
                    if (physComp.Enabled && (filterMask & (CollisionFilterGroupFlags)physComp.CollisionGroup) != 0)
                    {
                        return true;
                    }
                }
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowTest(CollidableReference collidable, int childIndex)
            {
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnRayHit(in RayData ray, ref float maximumT, float t, in System.Numerics.Vector3 normal, CollidableReference collidable, int childIndex)
            {
                var nativeResult = new BepuNativeRaycastResult
                {
                    RayData = ray,
                    T = t,
                    MaximumT = maximumT,
                    Normal = normal,
                    Collidable = collidable,
                    ChildIndex = childIndex
                };
                HitResults.Add(nativeResult.ToHitResult(xenkoSimulation));
            }
        }

        private struct BepuRaycastClosestHitHandler : IRayHitHandler
        {
            private readonly BepuSimulation xenkoSimulation;
            private readonly CollisionFilterGroups filterGroup;
            private readonly CollisionFilterGroupFlags filterMask;
            private readonly float rayMaxLength;

            private float currentClosestT;
            private BepuNativeRaycastResult closestHit;

            public BepuHitResult Result => currentClosestT == float.PositiveInfinity ? BepuHitResult.NoHit : closestHit.ToHitResult(xenkoSimulation);

            /// <param name="filterGroup">The collision group of this raycast</param>
            /// <param name="filterFlags">The collision group that this raycast can collide with</param>
            public BepuRaycastClosestHitHandler(
                BepuSimulation xenkoSimulation,
                float rayMaxLength,
                CollisionFilterGroups filterGroup = DefaultGroup,
                CollisionFilterGroupFlags filterMask = DefaultFlags) : this()
            {
                this.xenkoSimulation = xenkoSimulation;
                this.filterGroup = filterGroup;
                this.filterMask = filterMask;
                this.rayMaxLength = rayMaxLength;
                currentClosestT = float.PositiveInfinity;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowTest(CollidableReference collidable)
            {
                if (xenkoSimulation.collidableReferenceToComponent.TryGetValue(collidable, out var physComp))
                {
                    if (physComp.Enabled && (filterMask & (CollisionFilterGroupFlags)physComp.CollisionGroup) != 0)
                    {
                        return true;
                    }
                }
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowTest(CollidableReference collidable, int childIndex)
            {
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnRayHit(in RayData ray, ref float maximumT, float t, in System.Numerics.Vector3 normal, CollidableReference collidable, int childIndex)
            {
                // We can reduce maximumT so that we don't need to count collisions that occur greater than the new maximumT.
                if (t < maximumT)
                {
                    maximumT = t;
                }
                if (t < currentClosestT)
                {
                    currentClosestT = t;
                    closestHit = new BepuNativeRaycastResult
                    {
                        RayData = ray,
                        T = t,
                        MaximumT = rayMaxLength,
                        Normal = normal,
                        Collidable = collidable,
                        ChildIndex = childIndex
                    };
                }
            }
        }

        private class BepuHitResultListPooledObjectPolicy : IPooledObjectPolicy<FastList<BepuHitResult>>
        {
            public static readonly BepuHitResultListPooledObjectPolicy Instance = new BepuHitResultListPooledObjectPolicy();

            public FastList<BepuHitResult> Create()
            {
                return new FastList<BepuHitResult>();
            }

            public bool Return(FastList<BepuHitResult> obj)
            {
                obj.Clear();
                return true;
            }
        }
    }
}
