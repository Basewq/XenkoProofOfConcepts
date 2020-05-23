using System;
using System.Linq;
using Xenko.Core;
using Xenko.Core.Serialization.Contents;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    [ContentSerializer(typeof(DataContentSerializer<BepuColliderShapeAssetDesc>))]
    [DataContract("BepuColliderShapeAssetDesc")]
    [Display(50, "Asset (Bepu)")]
    public class BepuColliderShapeAssetDesc : IBepuInlineColliderShapeDesc
    {
        /// <userdoc>
        /// The reference to the collider Shape asset.
        /// </userdoc>
        [DataMember(10)]
        public BepuPhysicsColliderShape Shape { get; set; }

        public bool Match(object obj)
        {
            var other = obj as BepuColliderShapeAssetDesc;
            if (other == null) return false;

            if (other.Shape == null || Shape == null)
                return other.Shape == Shape;

            if (other.Shape.Descriptions == null || Shape.Descriptions == null)
                return other.Shape.Descriptions == Shape.Descriptions;

            if (other.Shape.Descriptions.Count != Shape.Descriptions.Count)
                return false;

            if (other.Shape.Descriptions.Where((t, i) => !t.Match(Shape.Descriptions[i])).Any())
                return false;

            // TODO: shouldn't we return true here?
            return other.Shape == Shape;
        }

        public BepuColliderShape CreateShape(BepuUtilities.Memory.BufferPool bufferPool)
        {
            if (Shape == null)
            {
                return null;
            }

            if (Shape.Shape == null)
            {
                Shape.Shape = BepuPhysicsColliderShape.Compose(Shape.Descriptions, bufferPool);
            }

            return Shape.Shape;
        }
    }
}
