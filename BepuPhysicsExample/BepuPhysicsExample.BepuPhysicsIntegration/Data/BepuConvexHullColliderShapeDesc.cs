using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization.Contents;
using Xenko.Physics;
using Xenko.Rendering;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    [ContentSerializer(typeof(DataContentSerializer<BepuConvexHullColliderShapeDesc>))]
    [DataContract("BepuConvexHullColliderShapeDesc")]
    [Display(500, "Convex Hull (Bepu)")]
    public class BepuConvexHullColliderShapeDesc : IBepuAssetColliderShapeDesc
    {
        [Display(Browsable = false)]
        [DataMember(10)]
        public List<List<List<Vector3>>> ConvexHulls; // Multiple meshes -> Multiple Hulls -> Hull points

        [Display(Browsable = false)]
        [DataMember(20)]
        public List<List<List<uint>>> ConvexHullsIndices; // Multiple meshes -> Multiple Hulls -> Hull tris

        /// <userdoc>
        /// Model asset from where the engine will derive the convex hull.
        /// </userdoc>
        [DataMember(30)]
        public Model Model;

        /// <userdoc>
        /// The offset with the real graphic mesh.
        /// </userdoc>
        [DataMember(31)]
        public Vector3 LocalOffset;

        /// <userdoc>
        /// The local rotation of the collider shape.
        /// </userdoc>
        [DataMember(32)]
        public Quaternion LocalRotation = Quaternion.Identity;

        /// <userdoc>
        /// The scaling of the generated convex hull.
        /// </userdoc>
        [DataMember(45)]
        public Vector3 Scaling = Vector3.One;

        /// <userdoc>
        /// If this is not checked, the contained parameters are ignored and only a simple convex hull of the model will be generated.
        /// </userdoc>
        [DataMember(50)]
        [NotNull]
        public ConvexHullDecompositionParameters Decomposition { get; set; } = new ConvexHullDecompositionParameters();

        public bool Match(object obj)
        {
            var other = obj as BepuConvexHullColliderShapeDesc;
            if (other == null)
                return false;

            if (other.LocalOffset != LocalOffset || other.LocalRotation != LocalRotation)
                return false;

            return other.Model == Model &&
                   other.Scaling == Scaling &&
                   other.Decomposition.Match(Decomposition);
        }

        public BepuColliderShape CreateShape(BepuUtilities.Memory.BufferPool bufferPool)
        {
            if (ConvexHulls == null) return null;
            BepuColliderShape shape;

            // Optimize performance and focus on less shapes creation since this shape could be nested

            if (ConvexHulls.Count == 1)
            {
                var curMeshConvexHulls = ConvexHulls[0];
                var curMeshConvexHullsIndices = ConvexHullsIndices[0];
                if (curMeshConvexHulls.Count == 1 && curMeshConvexHullsIndices[0].Count > 0)
                {
                    shape = new BepuConvexHullColliderShape(curMeshConvexHulls[0], curMeshConvexHullsIndices[0], Scaling, bufferPool)
                    {
                        NeedsCustomCollisionCallback = true,
                    };

                    //shape.UpdateLocalTransformations();
                    shape.Description = this;

                    return shape;
                }

                if (curMeshConvexHulls.Count <= 1) return null;

                var subCompound = new BepuCompoundColliderShape
                {
                    NeedsCustomCollisionCallback = true,
                };

                for (int i = 0; i < curMeshConvexHulls.Count; i++)
                {
                    var verts = curMeshConvexHulls[i];
                    var indices = curMeshConvexHullsIndices[i];

                    if (indices.Count == 0) continue;

                    var subHull = new BepuConvexHullColliderShape(verts, indices, Scaling, bufferPool);
                    //subHull.UpdateLocalTransformations();
                    subCompound.AddChildShape(subHull);
                }

                //subCompound.UpdateLocalTransformations();
                subCompound.Description = this;

                return subCompound;
            }

            if (ConvexHulls.Count <= 1) return null;

            var compound = new BepuCompoundColliderShape
            {
                NeedsCustomCollisionCallback = true,
            };

            for (int i = 0; i < ConvexHulls.Count; i++)
            {
                var curMeshConvexHulls = ConvexHulls[i];
                var curMeshConvexHullsIndices = ConvexHullsIndices[i];

                if (curMeshConvexHulls.Count == 1)
                {
                    if (curMeshConvexHullsIndices[0].Count == 0) continue;

                    var subHull = new BepuConvexHullColliderShape(curMeshConvexHulls[0], curMeshConvexHullsIndices[0], Scaling, bufferPool);
                    //subHull.UpdateLocalTransformations();
                    compound.AddChildShape(subHull);
                }
                else if (curMeshConvexHulls.Count > 1)
                {
                    var subCompound = new BepuCompoundColliderShape();

                    // Loop through each hulls
                    for (int b = 0; b < curMeshConvexHulls.Count; b++)
                    {
                        var verts = curMeshConvexHulls[b];
                        var indices = curMeshConvexHullsIndices[b];

                        if (indices.Count == 0) continue;

                        var subHull = new BepuConvexHullColliderShape(verts, indices, Scaling, bufferPool);
                        //subHull.UpdateLocalTransformations();
                        subCompound.AddChildShape(subHull);
                    }

                    //subCompound.UpdateLocalTransformations();

                    compound.AddChildShape(subCompound);
                }
            }

            //compound.UpdateLocalTransformations();
            compound.Description = this;

            return compound;
        }
    }
}
