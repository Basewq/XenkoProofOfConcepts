using BepuPhysics.Collidables;
using System;
using Stride.Core.Mathematics;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Physics;
using Stride.Rendering;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    public class BepuCylinderColliderShape : BepuColliderShape
    {
        //public readonly ShapeOrientation Orientation;
        public readonly float Height;
        public readonly float Radius;

        /// <summary>
        /// Initializes a new instance of the <see cref="BepuCylinderColliderShape"/> class.
        /// </summary>
        /// <param name="radius">The radius of the cylinder</param>
        /// <param name="height">The height of the cylinder</param>
        public BepuCylinderColliderShape(float height, float radius)
        {
            Type = ColliderShapeTypes.Cylinder;
            //Is2D = false; //always false for cylinders
            Height = height;
            Radius = radius;

            cachedScaling = Vector3.One;
            //Orientation = orientationParam;

            InternalShape = new Cylinder(Radius, Height);

            DebugPrimitiveMatrix = Matrix.Scaling(new Vector3(Radius * 2, Height, Radius * 2) * DebugScaling);
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            return GeometricPrimitive.Cylinder.New(device).ToMeshDraw();
        }

        internal override void CreateAndAddCollidableDescription(
            BepuPhysicsComponent physicsComponent,
            BepuSimulation xenkoSimulation,
            out TypedIndex shapeTypeIndex,
            out CollidableDescription collidableDescription)
        {
            CreateAndAddCollidableDescription<Cylinder>(
                physicsComponent, xenkoSimulation, out shapeTypeIndex, out collidableDescription);
        }
    }
}
