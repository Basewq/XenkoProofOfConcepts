using System;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization.Contents;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    [ContentSerializer(typeof(DataContentSerializer<BepuStaticPlaneColliderShapeDesc>))]
    [DataContract("BepuStaticPlaneColliderShapeDesc")]
    [Display(50, "Infinite Plane (Bepu)")]
    public class BepuStaticPlaneColliderShapeDesc : IBepuInlineColliderShapeDesc
    {
        /// <userdoc>
        /// The normal of the infinite plane.
        /// </userdoc>
        [DataMember(10)]
        public Vector3 Normal = Vector3.UnitY;

        /// <userdoc>
        /// The distance offset.
        /// </userdoc>
        [DataMember(20)]
        public float Offset;

        public bool Match(object obj)
        {
            var other = obj as BepuStaticPlaneColliderShapeDesc;
            if (other == null) return false;
            return other.Normal == Normal && Math.Abs(other.Offset - Offset) < float.Epsilon;
        }

        public BepuColliderShape CreateShape(BepuUtilities.Memory.BufferPool bufferPool)
        {
            return new BepuStaticPlaneColliderShape(Normal, Offset, bufferPool);
        }
    }
}
