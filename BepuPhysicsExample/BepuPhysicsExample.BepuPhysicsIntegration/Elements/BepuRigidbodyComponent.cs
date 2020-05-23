using BepuPhysics;
using BepuPhysics.Collidables;
using System;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Physics;
using Xenko.Rendering;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    [DataContract("BepuRigidbodyComponent")]
    [Display("Rigidbody (Bepu)")]
    public sealed class BepuRigidbodyComponent : BepuPhysicsSkinnedComponentBase
    {
        [DataMemberIgnore]
        internal System.Numerics.Vector3 InternalGravity;

        //[DataMemberIgnore]
        //internal BulletSharp.RigidBody InternalRigidBody;
        //
        //[DataMemberIgnore]
        //internal XenkoMotionState MotionState;

        private float mass = 1.0f;
        private RigidBodyTypes type;
        private Vector3 gravity = Vector3.Zero;
        private float angularDamping;
        private float linearDamping;

        /// <summary>
        /// Gets the linked constraints.
        /// </summary>
        /// <value>
        /// The linked constraints.
        /// </value>
        [DataMemberIgnore]
        public List<BepuConstraint> LinkedConstraints { get; }

        public BepuRigidbodyComponent()
        {
            LinkedConstraints = new List<BepuConstraint>();
            ProcessCollisions = true;
        }

        [DataMemberIgnore]
        public override Matrix PhysicsWorldTransform
        {
            get
            {
                if (!NativeBodyReference.Exists)
                {
                    return Matrix.Identity;
                }
                var worldMatrix = Matrix.AffineTransformation(
                    scaling: 1,
                    rotation: NativeBodyReference.Pose.Orientation.ToXenkoQuaternion(),
                    translation: NativeBodyReference.Pose.Position.ToXenkoVector3());
                return worldMatrix;
            }
            set
            {
                value.Decompose(out _, out Quaternion rotation, out Vector3 translation);
                ref var pose = ref NativeBodyReference.Pose;
                pose.Position = translation.ToNumericsVector3();
                pose.Orientation = rotation.ToBepuQuaternion();
            }
        }

        /// <summary>
        /// Gets or sets the kinematic property
        /// </summary>
        /// <value>true, false</value>
        /// <userdoc>
        /// Move the rigidbody only by the transform property, not other forces
        /// </userdoc>
        [DataMember(75)]
        public bool IsKinematic
        {
            get => RigidBodyType == RigidBodyTypes.Kinematic;
            set
            {
                RigidBodyType = value ? RigidBodyTypes.Kinematic : RigidBodyTypes.Dynamic;
            }
        }

        /// <summary>
        /// Gets or sets the mass of this Rigidbody
        /// </summary>
        /// <value>
        /// true, false
        /// </value>
        /// <userdoc>
        /// Objects with higher mass push objects with lower mass more when they collide. For large differences, use point values; for example, write 0.1 or 10, not 1 or 100000.
        /// </userdoc>
        [DataMember(80)]
        [DataMemberRange(0, 6)]
        public float Mass
        {
            get => mass;
            set
            {
                if (value < 0)
                {
                    throw new InvalidOperationException("the Mass of a Rigidbody cannot be negative.");
                }

                mass = value;

                if (!NativeBodyReference.Exists) return;

                BodyInertia bodyInertia;
                if (ColliderShape.InternalShape is IConvexShape convexShape)
                {
                    convexShape.ComputeInertia(mass, out bodyInertia);
                }
                else
                {
                    bodyInertia = new BodyInertia
                    {
                        InverseMass = 1 / mass
                    };
                }
                NativeBodyReference.SetLocalInertia(bodyInertia);

                //var inertia = ColliderShape.InternalShape.CalculateLocalInertia(value);
                //InternalRigidBody.SetMassProps(value, inertia);
                //InternalRigidBody.UpdateInertiaTensor(); //this was the major headache when I had to debug Slider and Hinge constraint
            }
        }

        /// <summary>
        /// Gets or sets the linear damping of this rigidbody
        /// </summary>
        /// <value>
        /// true, false
        /// </value>
        /// <userdoc>
        /// The amount of damping for directional forces
        /// </userdoc>
        [DataMember(85)]
        public float LinearDamping
        {
            get => linearDamping;
            set
            {
                linearDamping = value;

                //InternalRigidBody?.SetDamping(value, AngularDamping);
            }
        }

        /// <summary>
        /// Gets or sets the angular damping of this rigidbody
        /// </summary>
        /// <value>
        /// true, false
        /// </value>
        /// <userdoc>
        /// The amount of damping for rotational forces
        /// </userdoc>
        [DataMember(90)]
        public float AngularDamping
        {
            get => angularDamping;
            set
            {
                angularDamping = value;

                //InternalRigidBody?.SetDamping(LinearDamping, value);
            }
        }

        /// <summary>
        /// Gets or sets if this Rigidbody overrides world gravity
        /// </summary>
        /// <value>
        /// true, false
        /// </value>
        /// <userdoc>
        /// Override gravity with the vector specified in Gravity
        /// </userdoc>
        [DataMember(95)]
        public bool OverrideGravity { get; set; }

        /// <summary>
        /// Gets or sets the gravity acceleration applied to this RigidBody
        /// </summary>
        /// <value>
        /// A vector representing moment and direction
        /// </value>
        /// <userdoc>
        /// The gravity acceleration applied to this rigidbody
        /// </userdoc>
        [DataMember(100)]
        public Vector3 Gravity
        {
            get => gravity;
            set
            {
                gravity = value;
                InternalGravity = value.ToNumericsVector3();
            }
        }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [DataMemberIgnore]
        public RigidBodyTypes RigidBodyType
        {
            get => type;
            set
            {
                type = value;

                //if (!NativeBodyReference.Exists)
                //{
                //    return;
                //}

                //switch (value)
                //{
                //    case RigidBodyTypes.Dynamic:
                //        InternalRigidBody.CollisionFlags &= ~(BulletSharp.CollisionFlags.StaticObject | BulletSharp.CollisionFlags.KinematicObject);
                //        break;

                //    case RigidBodyTypes.Static:
                //        InternalRigidBody.CollisionFlags &= ~BulletSharp.CollisionFlags.KinematicObject;
                //        InternalRigidBody.CollisionFlags |= BulletSharp.CollisionFlags.StaticObject;
                //        break;

                //    case RigidBodyTypes.Kinematic:
                //        InternalRigidBody.CollisionFlags &= ~BulletSharp.CollisionFlags.StaticObject;
                //        InternalRigidBody.CollisionFlags |= BulletSharp.CollisionFlags.KinematicObject;
                //        break;

                //    default:
                //        throw new NotSupportedException(nameof(value));
                //}
                //if (!OverrideGravity)
                //{
                //    if (value == RigidBodyTypes.Dynamic)
                //    {
                //        InternalRigidBody.Gravity = Simulation.Gravity;
                //    }
                //    else
                //    {
                //        InternalRigidBody.Gravity = Vector3.Zero;
                //    }
                //}
                //InternalRigidBody.InterpolationAngularVelocity = Vector3.Zero;
                //InternalRigidBody.LinearVelocity = Vector3.Zero;
                //InternalRigidBody.InterpolationAngularVelocity = Vector3.Zero;
                //InternalRigidBody.AngularVelocity = Vector3.Zero;
            }
        }

        protected override void OnColliderShapeChanged(BepuColliderShape oldColliderShape, BepuColliderShape newColliderShape)
        {
            if (NativeBodyReference.Exists)
            {
                // Update existing body's shape

                //NativeCollisionObject.CollisionShape = value.InternalShape;
                if (newColliderShape != null)
                {
                    var collidableType = RigidBodyType.ToBepuCollidableType();
                    Simulation.SetRigidBody(this, collidableType);
                }
                else
                {
                    Simulation.RemoveRigidBody(this);
                }
            }

            //var inertia = colliderShape.InternalShape.CalculateLocalInertia(mass);
            //InternalRigidBody.SetMassProps(mass, inertia);
            //InternalRigidBody.UpdateInertiaTensor(); //this was the major headache when I had to debug Slider and Hinge constraint
        }

        protected override void OnAttach()
        {
            //MotionState = new XenkoMotionState(this);

            SetupBoneLink();

            //var rbci = new BulletSharp.RigidBodyConstructionInfo(0.0f, MotionState, ColliderShape.InternalShape, Vector3.Zero);
            //InternalRigidBody = new BulletSharp.RigidBody(rbci)
            //{
            //    UserObject = this,
            //};

            //NativeCollisionObject = InternalRigidBody;

            //NativeCollisionObject.ContactProcessingThreshold = !Simulation.CanCcd ? 1e18f : 1e30f;

            //if (ColliderShape.NeedsCustomCollisionCallback)
            //{
            //    NativeCollisionObject.CollisionFlags |= BulletSharp.CollisionFlags.CustomMaterialCallback;
            //}

            //if (ColliderShape.Is2D) //set different defaults for 2D shapes
            //{
            //    InternalRigidBody.LinearFactor = new Vector3(1.0f, 1.0f, 0.0f);
            //    InternalRigidBody.AngularFactor = new Vector3(0.0f, 0.0f, 1.0f);
            //}

            //var inertia = ColliderShape.InternalShape.CalculateLocalInertia(mass);
            //InternalRigidBody.SetMassProps(mass, inertia);
            //InternalRigidBody.UpdateInertiaTensor(); //this was the major headache when I had to debug Slider and Hinge constraint

            base.OnAttach();

            RigidBodyType = IsKinematic ? RigidBodyTypes.Kinematic : RigidBodyTypes.Dynamic;

            var collidableType = RigidBodyType.ToBepuCollidableType();
            Simulation.SetRigidBody(this, collidableType);

            UpdatePhysicsTransformation(); // This will set position and rotation of the collider
        }

        protected override void OnDetach()
        {
            //MotionState.Dispose();
            //MotionState.Clear();

            if (!NativeCollidableReference.IsSet)
                return;

            //Remove constraints safely
            var toremove = new FastList<BepuConstraint>();
            foreach (var c in LinkedConstraints)
            {
                toremove.Add(c);
            }

            foreach (var disposable in toremove)
            {
                disposable.Dispose();
            }

            LinkedConstraints.Clear();
            //~Remove constraints

            Simulation.RemoveRigidBody(this);

            //InternalRigidBody = null;

            base.OnDetach();
        }

        protected internal override void OnUpdateDraw()
        {
            base.OnUpdateDraw();

            if (type == RigidBodyTypes.Dynamic && BoneIndex != -1)
            {
                //write to ModelViewHierarchy
                var model = Data.ModelComponent;
                model.Skeleton.NodeTransformations[BoneIndex].Flags = !IsKinematic ? ModelNodeFlags.EnableRender | ModelNodeFlags.OverrideWorldMatrix : ModelNodeFlags.Default;
                if (!IsKinematic) model.Skeleton.NodeTransformations[BoneIndex].WorldMatrix = BoneWorldMatrixOut;
            }
        }

        // This is called by the physics system to update the transformation of Dynamic rigidbodies.
        internal void RigidBodySetWorldTransform(ref Matrix physicsTransform)
        {
            Simulation.SimulationProfiler.Mark();
            Simulation.UpdatedRigidbodies++;

            if (BoneIndex == -1)
            {
                UpdateTransformationComponent(ref physicsTransform);
            }
            else
            {
                UpdateBoneTransformation(ref physicsTransform);
            }
        }

        // This is valid for Dynamic rigidbodies (called once at initialization)
        // and Kinematic rigidbodies, called every simulation tick (if body not sleeping) to let the physics engine know where the kinematic body is.
        internal void RigidBodyGetWorldTransform(out Matrix physicsTransform)
        {
            Simulation.SimulationProfiler.Mark();
            Simulation.UpdatedRigidbodies++;

            if (BoneIndex == -1)
            {
                DerivePhysicsTransformation(out physicsTransform);
            }
            else
            {
                DeriveBonePhysicsTransformation(out physicsTransform);
            }
        }

        ///// <summary>
        ///// Gets the total torque.
        ///// </summary>
        ///// <value>
        ///// The total torque.
        ///// </value>
        //public Vector3 TotalTorque => InternalRigidBody?.TotalTorque ?? Vector3.Zero;

        /// <summary>
        /// Applies the impulse.
        /// </summary>
        /// <param name="impulse">The impulse.</param>
        public void ApplyImpulse(Vector3 impulse)
        {
            if (!NativeBodyReference.Exists)
            {
                throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
            }

            NativeBodyReference.ApplyLinearImpulse(impulse.ToNumericsVector3());
            //InternalRigidBody.ApplyCentralImpulse(impulse);
        }

        /// <summary>
        /// Applies the impulse.
        /// </summary>
        /// <param name="impulse">The impulse.</param>
        /// <param name="localOffset">The local offset.</param>
        public void ApplyImpulse(Vector3 impulse, Vector3 localOffset)
        {
            if (!NativeBodyReference.Exists)
            {
                throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
            }

            NativeBodyReference.ApplyImpulse(impulse.ToNumericsVector3(), localOffset.ToNumericsVector3());
            //InternalRigidBody.ApplyImpulse(impulse, localOffset);
        }

        /// <summary>
        /// Applies the force.
        /// </summary>
        /// <param name="force">The force.</param>
        public void ApplyForce(Vector3 force)
        {
            if (!NativeBodyReference.Exists)
            {
                throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
            }

            // ??
            //NativeBodyReference.ApplyImpulse(impulse.ToNumericsVector3(), localOffset.ToNumericsVector3());
            //InternalRigidBody.ApplyCentralForce(force);
        }

        /// <summary>
        /// Applies the force.
        /// </summary>
        /// <param name="force">The force.</param>
        /// <param name="localOffset">The local offset.</param>
        public void ApplyForce(Vector3 force, Vector3 localOffset)
        {
            //if (!NativeBodyReference.Exists)
            //{
            //    throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
            //}

            //InternalRigidBody.ApplyForce(force, localOffset);
        }

        /// <summary>
        /// Applies the torque.
        /// </summary>
        /// <param name="torque">The torque.</param>
        public void ApplyTorque(Vector3 torque)
        {
            //if (!NativeBodyReference.Exists)
            //{
            //    throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
            //}

            //InternalRigidBody.ApplyTorque(torque);
        }

        /// <summary>
        /// Applies the torque impulse.
        /// </summary>
        /// <param name="torque">The torque.</param>
        public void ApplyTorqueImpulse(Vector3 torque)
        {
            //if (!NativeBodyReference.Exists)
            //{
            //    throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
            //}

            //InternalRigidBody.ApplyTorqueImpulse(torque);
        }

        /// <summary>
        /// Clears all forces being applied to this rigidbody
        /// </summary>
        public void ClearForces()
        {
            //if (!NativeBodyReference.Exists)
            //{
            //    throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
            //}

            //InternalRigidBody?.ClearForces();
            //InternalRigidBody.InterpolationAngularVelocity = Vector3.Zero;
            //InternalRigidBody.LinearVelocity = Vector3.Zero;
            //InternalRigidBody.InterpolationAngularVelocity = Vector3.Zero;
            //InternalRigidBody.AngularVelocity = Vector3.Zero;
        }

        /// <summary>
        /// Gets or sets the angular velocity.
        /// </summary>
        /// <value>
        /// The angular velocity.
        /// </value>
        [DataMemberIgnore]
        public Vector3 AngularVelocity
        {
            get
            {
                return NativeBodyReference.Exists ? NativeBodyReference.Velocity.Angular.ToXenkoVector3() : Vector3.Zero;
            }
            set
            {
                if (!NativeBodyReference.Exists)
                {
                    throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
                }

                NativeBodyReference.Velocity.Angular = value.ToNumericsVector3();
            }
        }

        /// <summary>
        /// Gets or sets the linear velocity.
        /// </summary>
        /// <value>
        /// The linear velocity.
        /// </value>
        [DataMemberIgnore]
        public Vector3 LinearVelocity
        {
            get => NativeBodyReference.Exists ? NativeBodyReference.Velocity.Linear.ToXenkoVector3() : Vector3.Zero;
            set
            {
                if (!NativeBodyReference.Exists)
                {
                    throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
                }


                NativeBodyReference.Velocity.Linear = value.ToNumericsVector3();
            }
        }

        /// <summary>
        /// Gets the total force.
        /// </summary>
        /// <value>
        /// The total force.
        /// </value>
        //public Vector3 TotalForce => NativeBodyReference.Exists ? NativeBodyReference.LocalInertia.InverseMass InternalRigidBody?.TotalForce ?? Vector3.Zero;

        /// <summary>
        /// Gets or sets the angular factor.
        /// </summary>
        /// <value>
        /// The angular factor.
        /// </value>
        [DataMemberIgnore]
        public Vector3 AngularFactor
        {
            get => /*InternalRigidBody?.AngularFactor ?? */Vector3.Zero;
            set
            {
                if (!NativeBodyReference.Exists)
                {
                    throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
                }

                //InternalRigidBody.AngularFactor = value;
            }
        }

        /// <summary>
        /// Gets or sets the linear factor.
        /// </summary>
        /// <value>
        /// The linear factor.
        /// </value>
        [DataMemberIgnore]
        public Vector3 LinearFactor
        {
            get => /*InternalRigidBody?.LinearFactor ??*/ Vector3.Zero;
            set
            {
                if (!NativeBodyReference.Exists)
                {
                    throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
                }

                //InternalRigidBody.LinearFactor = value;
            }
        }

        //internal class XenkoMotionState : BulletSharp.MotionState
        //{
        //    private BepuRigidbodyComponent rigidBody;

        //    public XenkoMotionState(BepuRigidbodyComponent rb)
        //    {
        //        rigidBody = rb;
        //    }

        //    public void Clear()
        //    {
        //        rigidBody = null;
        //    }

        //    public override void GetWorldTransform(out BulletSharp.Math.Matrix transform)
        //    {
        //        rigidBody.RigidBodyGetWorldTransform(out var xenkoMatrix);
        //        transform = xenkoMatrix;
        //    }

        //    public override void SetWorldTransform(ref BulletSharp.Math.Matrix transform)
        //    {
        //        Matrix asXenkoMatrix = transform;
        //        rigidBody.RigidBodySetWorldTransform(ref asXenkoMatrix);
        //    }
        //}
    }
}
