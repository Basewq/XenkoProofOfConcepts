using BepuPhysics.Collidables;
using Xenko.Core.Mathematics;
using Xenko.Extensions;
using Xenko.Graphics;
using Xenko.Graphics.GeometricPrimitives;
using Xenko.Physics;
using Xenko.Rendering;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    public class BepuBoxColliderShape : BepuColliderShape
    {
        public readonly Vector3 BoxSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="BepuBoxColliderShape"/> class.
        /// </summary>
        /// <param name="size">The size of the cube. X = Width, Y = Height, Z = Length</param>
        public BepuBoxColliderShape(Vector3 size)
        {
            Type = ColliderShapeTypes.Box;
            //Is2D = is2D;
            BoxSize = size;

            cachedScaling = Vector3.One;
            //cachedScaling = Is2D ? new Vector3(1, 1, 0.001f) : Vector3.One;

            //if (is2D) size.Z = 0.001f;

            InternalShape = new Box(size.X, size.Y, size.Z);

            DebugPrimitiveMatrix = Matrix.Scaling(size * DebugScaling);
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            return GeometricPrimitive.Cube.New(device).ToMeshDraw();
        }

        internal override void CreateAndAddCollidableDescription(
            BepuPhysicsComponent physicsComponent,
            BepuSimulation xenkoSimulation,
            out TypedIndex shapeTypeIndex,
            out CollidableDescription collidableDescription)
        {
            CreateAndAddCollidableDescription<Box>(
                physicsComponent, xenkoSimulation, out shapeTypeIndex, out collidableDescription);
        }
    }
}
