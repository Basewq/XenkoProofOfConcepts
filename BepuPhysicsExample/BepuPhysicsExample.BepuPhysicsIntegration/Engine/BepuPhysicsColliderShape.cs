using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Engine.Design;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    [DataContract]
    [ContentSerializer(typeof(DataContentSerializer<BepuPhysicsColliderShape>))]
    [DataSerializerGlobal(typeof(CloneSerializer<BepuPhysicsColliderShape>), Profile = "Clone")]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<BepuPhysicsColliderShape>), Profile = "Content")]
    public class BepuPhysicsColliderShape : IDisposable
    {
        public BepuPhysicsColliderShape()
        {
        }

        public BepuPhysicsColliderShape([NotNull] IEnumerable<IBepuAssetColliderShapeDesc> descriptions)
        {
            Descriptions.AddRange(descriptions);
        }

        /// <summary>
        /// Used to serialize one or more collider shapes into one single shape
        /// Reading this value will automatically parse the Shape property into its description
        /// Writing this value will automatically compose, create and populate the Shape property
        /// </summary>
        [DataMember]
        public List<IBepuAssetColliderShapeDesc> Descriptions { get; } = new List<IBepuAssetColliderShapeDesc>();

        [DataMemberIgnore]
        public BepuColliderShape Shape { get; internal set; }

        [NotNull]
        public static BepuPhysicsColliderShape New([NotNull] params IBepuAssetColliderShapeDesc[] descriptions)
        {
            if (descriptions == null) throw new ArgumentNullException(nameof(descriptions));
            return new BepuPhysicsColliderShape(descriptions);
        }

        internal static BepuColliderShape Compose(IReadOnlyList<IBepuAssetColliderShapeDesc> descs, BepuUtilities.Memory.BufferPool bufferPool)
        {
            if (descs == null)
            {
                return null;
            }

            BepuColliderShape res = null;

            if (descs.Count == 1) // Single shape case
            {
                res = CreateShape(descs[0], bufferPool);
                if (res == null) return null;
                res.IsPartOfAsset = true;
            }
            else if (descs.Count > 1) // Need a compound shape in this case
            {
                var compound = new BepuCompoundColliderShape();
                foreach (var desc in descs)
                {
                    var subShape = CreateShape(desc, bufferPool);
                    if (subShape == null) continue;
                    compound.AddChildShape(subShape);
                }
                res = compound;
                res.IsPartOfAsset = true;
            }

            return res;
        }

        internal static BepuColliderShape CreateShape(IBepuColliderShapeDesc desc, BepuUtilities.Memory.BufferPool bufferPool)
        {
            if (desc == null)
                return null;

            BepuColliderShape shape = desc.CreateShape(bufferPool);

            if (shape == null) return null;

            //shape.UpdateLocalTransformations();
            shape.Description = desc;

            return shape;
        }

        public void Dispose()
        {
            if (Shape == null) return;

            var compound = Shape.Parent;
            compound?.RemoveChildShape(Shape);

            Shape.Dispose();
            Shape = null;
        }
    }
}
