using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    [ContentSerializer(typeof(DataContentSerializer<BepuBoxColliderShapeDesc>))]
    [DataContract("BepuBoxColliderShapeDesc")]
    [Display(50, "Box (Bepu)")]
    public class BepuBoxColliderShapeDesc : IBepuInlineColliderShapeDesc
    {
        ///// <userdoc>
        ///// Select this if this shape will represent a Circle 2D shape
        ///// </userdoc>
        //[DataMember(5)]
        //public bool Is2D;

        /// <userdoc>
        /// The size of one edge of the box.
        /// </userdoc>
        [DataMember(10)]
        public Vector3 Size = Vector3.One;

        /// <userdoc>
        /// The offset with the real graphic mesh.
        /// </userdoc>
        [DataMember(20)]
        public Vector3 LocalOffset;

        /// <userdoc>
        /// The local rotation of the collider shape.
        /// </userdoc>
        [DataMember(30)]
        public Quaternion LocalRotation = Quaternion.Identity;

        public bool Match(object obj)
        {
            var other = obj as BepuBoxColliderShapeDesc;
            return other.Size == Size && other.LocalOffset == LocalOffset && other.LocalRotation == LocalRotation;
        }

        public BepuColliderShape CreateShape(BepuUtilities.Memory.BufferPool bufferPool)
        {
            return new BepuBoxColliderShape(Size) { LocalOffset = LocalOffset, LocalRotation = LocalRotation };
        }
    }
}
