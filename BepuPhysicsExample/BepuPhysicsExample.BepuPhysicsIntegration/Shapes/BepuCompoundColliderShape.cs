using BepuPhysics.Collidables;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Physics;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    public class BepuCompoundColliderShape : BepuColliderShape
    {
        //    /// <summary>
        //    /// Initializes a new instance of the <see cref="BepuCompoundColliderShape"/> class.
        //    /// </summary>
        //    public BepuCompoundColliderShape()
        //    {
        //        Type = ColliderShapeTypes.Compound;
        //        //Is2D = false;

        //        cachedScaling = Vector3.One;
        //        InternalShape = InternalCompoundShape = new CompoundShape
        //        {
        //            LocalScaling = cachedScaling,
        //        };
        //    }

        //    /// <summary>
        //    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        //    /// </summary>
        //    public override void Dispose()
        //    {
        //        foreach (var shape in colliderShapes)
        //        {
        //            InternalCompoundShape.RemoveChildShape(shape.InternalShape);

        //            if (!shape.IsPartOfAsset)
        //            {
        //                shape.Dispose();
        //            }
        //            else
        //            {
        //                shape.Parent = null;
        //            }
        //        }
        //        colliderShapes.Clear();

        //        base.Dispose();
        //    }

        private readonly List<BepuColliderShape> colliderShapes = new List<BepuColliderShape>();

        //    private ICompoundShape internalCompoundShape;

        //    internal ICompoundShape InternalCompoundShape
        //    {
        //        get
        //        {

        //            return internalCompoundShape;
        //        }
        //        set
        //        {
        //            InternalShape = internalCompoundShape = value;
        //        }
        //    }

        /// <summary>
        /// Adds a child shape.
        /// </summary>
        /// <param name="shape">The shape.</param>
        public void AddChildShape(BepuColliderShape shape)
        {
            //        colliderShapes.Add(shape);

            //        var compoundMatrix = Matrix.RotationQuaternion(shape.LocalRotation) * Matrix.Translation(shape.LocalOffset);

            //        InternalCompoundShape.AddChildShape(compoundMatrix, shape.InternalShape);

            //        shape.Parent = this;
        }

        /// <summary>
        /// Removes a child shape.
        /// </summary>
        /// <param name="shape">The shape.</param>
        public void RemoveChildShape(BepuColliderShape shape)
        {
            //        colliderShapes.Remove(shape);

            //        InternalCompoundShape.RemoveChildShape(shape.InternalShape);

            //        shape.Parent = null;
        }

        /// <summary>
        /// Gets the <see cref="BepuColliderShape"/> with the specified i.
        /// </summary>
        /// <value>
        /// The <see cref="BepuColliderShape"/>.
        /// </value>
        /// <param name="i">The i.</param>
        /// <returns></returns>
        public BepuColliderShape this[int i] => colliderShapes[i];

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public int Count => colliderShapes.Count;

        //    public override Vector3 Scaling
        //    {
        //        get => cachedScaling;
        //        set
        //        {
        //            base.Scaling = value;

        //            foreach (var colliderShape in colliderShapes)
        //            {
        //                colliderShape.Scaling = cachedScaling;
        //            }
        //        }
        //    }

        internal override void CreateAndAddCollidableDescription(
            BepuPhysicsComponent physicsComponent, BepuSimulation xenkoSimulation, out TypedIndex shapeTypeIndex, out CollidableDescription collidableDescription)
        {
            //throw new System.NotImplementedException();
            shapeTypeIndex = default;
            collidableDescription = default;
        }
    }
}
