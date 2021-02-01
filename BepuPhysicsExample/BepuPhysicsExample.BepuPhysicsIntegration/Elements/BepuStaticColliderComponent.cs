using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Physics;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    [DataContract("BepuStaticColliderComponent")]
    [Display("Static collider (Bepu)")]
    public sealed class BepuStaticColliderComponent : BepuPhysicsTriggerComponentBase
    {
        [DataMemberIgnore]
        internal StaticReference NativeStaticReference;
        //[DataMemberIgnore]
        //internal StaticDescription NativeStaticDescription;

        [DataMemberIgnore]
        public override Matrix PhysicsWorldTransform
        {
            get
            {
                if (!NativeStaticReference.Exists)
                {
                    return Matrix.Identity;
                }
                var worldMatrix = Matrix.AffineTransformation(
                    scaling: 1,
                    rotation: NativeStaticReference.Pose.Orientation.ToXenkoQuaternion(),
                    translation: NativeStaticReference.Pose.Position.ToXenkoVector3());
                return worldMatrix;
            }
            set
            {
                value.Decompose(out _, out Quaternion rotation, out Vector3 translation);
                ref var pose = ref NativeStaticReference.Pose;
                pose.Position = translation.ToNumericsVector3();
                pose.Orientation = rotation.ToBepuQuaternion();
            }
        }

        internal void ClearNativeStaticReference()
        {
            NativeStaticReference = default;
        }

        protected override void OnColliderShapeChanged(BepuColliderShape oldColliderShape, BepuColliderShape newColliderShape)
        {
            if (NativeStaticReference.Exists)
            {
                if (colliderShape != null)
                {
                    Simulation.SetStaticCollider(this);
                }
                else
                {
                    Simulation.RemoveStaticCollider(this);
                }
            }
        }

        protected override void OnAttach()
        {
            //NativeCollisionObject = new BulletSharp.CollisionObject
            //{
            //    CollisionShape = ColliderShape.InternalShape,
            //    ContactProcessingThreshold = !Simulation.CanCcd ? 1e18f : 1e30f,
            //    UserObject = this,
            //};

            //NativeCollisionObject.CollisionFlags |= BulletSharp.CollisionFlags.NoContactResponse;

            //if (ColliderShape.NeedsCustomCollisionCallback)
            //{
            //    NativeCollisionObject.CollisionFlags |= BulletSharp.CollisionFlags.CustomMaterialCallback;
            //}

            //this will set all the properties in the native side object
            base.OnAttach();

            Simulation.SetStaticCollider(this);

            UpdatePhysicsTransformation(); // This will set position and rotation of the collider
        }

        protected override void OnDetach()
        {
            if (NativeStaticReference.Exists)
            {
                return;
            }

            Simulation.RemoveStaticCollider(this);

            base.OnDetach();
        }

        protected override void OnUpdateBones()
        {
            base.OnUpdateBones();
        }
    }
}
