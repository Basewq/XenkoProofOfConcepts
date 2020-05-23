using BepuPhysics.Collidables;
using System;
using Xenko.Core.Mathematics;
using Xenko.Extensions;
using Xenko.Graphics;
using Xenko.Graphics.GeometricPrimitives;
using Xenko.Physics;
using Xenko.Rendering;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    public class BepuCapsuleColliderShape : BepuColliderShape
    {
        public readonly float Length;
        public readonly float Radius;
        //public readonly ShapeOrientation Orientation;

        /// <summary>
        /// Initializes a new instance of the <see cref="BepuCapsuleColliderShape"/> class.
        /// </summary>
        /// <param name="radius">The radius.</param>
        /// <param name="length">The length of the capsule.</param>
        /// <param name="orientation">Up axis.</param>
        public BepuCapsuleColliderShape(float radius, float length)
        {
            Type = ColliderShapeTypes.Capsule;
            //Is2D = is2D;

            Length = length;
            Radius = radius;

            //cachedScaling = Is2D ? new Vector3(1, 1, 0) : Vector3.One;
            cachedScaling = Vector3.One;

            InternalShape = new Capsule(radius, length);

            //var rotation = Matrix.Identity;
            DebugPrimitiveMatrix = Matrix.Scaling(new Vector3(DebugScaling));// * rotation;
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            return GeometricPrimitive.Capsule.New(device, Length, Radius).ToMeshDraw();
        }

        internal override void CreateAndAddCollidableDescription(
            BepuPhysicsComponent physicsComponent,
            BepuSimulation xenkoSimulation,
            out TypedIndex shapeTypeIndex,
            out CollidableDescription collidableDescription)
        {
            CreateAndAddCollidableDescription<Capsule>(
                physicsComponent, xenkoSimulation, out shapeTypeIndex, out collidableDescription);
        }

        //public override Vector3 Scaling
        //{
        //    get { return base.Scaling; }
        //    set
        //    {
        //        base.Scaling = new Vector3(value.X, value.Y, value.X);
        //    }
        //}
    }
}
