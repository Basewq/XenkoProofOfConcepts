using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    [ContentSerializer(typeof(DataContentSerializer<BepuSphereColliderShapeDesc>))]
    [DataContract("BepuSphereColliderShapeDesc")]
    [Display(50, "Sphere (Bepu)")]
    public class BepuSphereColliderShapeDesc : IBepuInlineColliderShapeDesc
    {
        ///// <userdoc>
        ///// Select this if this shape will represent a Circle 2D shape
        ///// </userdoc>
        //[DataMember(10)]
        //public bool Is2D;

        /// <userdoc>
        /// The radius of the sphere/circle.
        /// </userdoc>
        [DataMember(20)]
        [DefaultValue(0.5f)]
        public float Radius = 0.5f;

        /// <userdoc>
        /// The offset with the real graphic mesh.
        /// </userdoc>
        [DataMember(30)]
        public Vector3 LocalOffset;

        public bool Match(object obj)
        {
            var other = obj as BepuSphereColliderShapeDesc;
            return Math.Abs(other.Radius - Radius) < float.Epsilon && other.LocalOffset == LocalOffset;
        }

        public BepuColliderShape CreateShape(BepuUtilities.Memory.BufferPool bufferPool)
        {
            return new BepuSphereColliderShape(Radius) { LocalOffset = LocalOffset };
        }
    }
}
