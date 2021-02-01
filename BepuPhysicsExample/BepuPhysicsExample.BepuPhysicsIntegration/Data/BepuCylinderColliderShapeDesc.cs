using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Physics;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    [ContentSerializer(typeof(DataContentSerializer<BepuCylinderColliderShapeDesc>))]
    [DataContract("BepuCylinderColliderShapeDesc")]
    [Display(50, "Cylinder (Bepu)")]
    public class BepuCylinderColliderShapeDesc : IBepuInlineColliderShapeDesc
    {
        /// <userdoc>
        /// The height of the cylinder
        /// </userdoc>
        [DataMember(10)]
        [DefaultValue(1f)]
        public float Height = 1f;

        /// <userdoc>
        /// The radius of the cylinder
        /// </userdoc>
        [DataMember(20)]
        [DefaultValue(0.5f)]
        public float Radius = 0.5f;

        ///// <userdoc>
        ///// The orientation of the cylinder.
        ///// </userdoc>
        //[DataMember(30)]
        //[DefaultValue(ShapeOrientation.UpY)]
        //public ShapeOrientation Orientation = ShapeOrientation.UpY;

        /// <userdoc>
        /// The offset with the real graphic mesh.
        /// </userdoc>
        [DataMember(40)]
        public Vector3 LocalOffset;

        /// <userdoc>
        /// The local rotation of the collider shape.
        /// </userdoc>
        [DataMember(50)]
        public Quaternion LocalRotation = Quaternion.Identity;

        public bool Match(object obj)
        {
            var other = obj as BepuCylinderColliderShapeDesc;
            if (other == null)
                return false;

            return Math.Abs(other.Height - Height) < float.Epsilon
                   && Math.Abs(other.Radius - Radius) < float.Epsilon
                   //&& other.Orientation == Orientation
                   && other.LocalOffset == LocalOffset
                   && other.LocalRotation == LocalRotation;
        }

        public BepuColliderShape CreateShape(BepuUtilities.Memory.BufferPool bufferPool)
        {
            return new BepuCylinderColliderShape(Height, Radius) { LocalOffset = LocalOffset, LocalRotation = LocalRotation };
        }
    }
}
