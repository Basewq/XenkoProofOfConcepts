using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    [ContentSerializer(typeof(DataContentSerializer<BepuCapsuleColliderShapeDesc>))]
    [DataContract("BepuCapsuleColliderShapeDesc")]
    [Display(50, "Capsule (Bepu)")]
    public class BepuCapsuleColliderShapeDesc : IBepuInlineColliderShapeDesc
    {
        ///// <userdoc>
        ///// Select this if this shape will represent a 2D shape
        ///// </userdoc>
        //[DataMember(10)]
        //[DefaultValue(false)]
        //public bool Is2D;

        /// <userdoc>
        /// The length of the capsule (distance between the center of the two sphere centers).
        /// </userdoc>
        [DataMember(20)]
        [DefaultValue(0.5f)]
        public float Length = 0.5f;

        /// <userdoc>
        /// The radius of the capsule.
        /// </userdoc>
        [DataMember(30)]
        [DefaultValue(0.25f)]
        public float Radius = 0.25f;

        ///// <userdoc>
        ///// The orientation of the capsule.
        ///// </userdoc>
        //[DataMember(40)]
        //[DefaultValue(ShapeOrientation.UpY)]
        //public ShapeOrientation Orientation = ShapeOrientation.UpY;

        /// <userdoc>
        /// The offset with the real graphic mesh.
        /// </userdoc>
        [DataMember(50)]
        public Vector3 LocalOffset;

        /// <userdoc>
        /// The local rotation of the collider shape.
        /// </userdoc>
        [DataMember(60)]
        public Quaternion LocalRotation = Quaternion.Identity;

        public bool Match(object obj)
        {
            var other = obj as BepuCapsuleColliderShapeDesc;
            return Math.Abs(other.Length - Length) < float.Epsilon &&
                   Math.Abs(other.Radius - Radius) < float.Epsilon &&
                   //other.Orientation == Orientation &&
                   other.LocalOffset == LocalOffset &&
                   other.LocalRotation == LocalRotation;
        }

        public BepuColliderShape CreateShape(BepuUtilities.Memory.BufferPool bufferPool)
        {
            return new BepuCapsuleColliderShape(Radius, Length) { LocalOffset = LocalOffset, LocalRotation = LocalRotation };
        }
    }
}
