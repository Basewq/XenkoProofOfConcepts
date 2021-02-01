using BepuPhysics.Collidables;
using Stride.Core.Mathematics;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Physics;
using Stride.Rendering;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    public class BepuSphereColliderShape : BepuColliderShape
    {
        public readonly float Radius;

        /// <summary>
        /// Initializes a new instance of the <see cref="BepuSphereColliderShape"/> class.
        /// </summary>
        /// <param name="radius">The radius.</param>
        public BepuSphereColliderShape(float radius)
        {
            Type = ColliderShapeTypes.Sphere;
            Radius = radius;

            //cachedScaling = Is2D ? new Vector3(1, 1, 0) : Vector3.One;
            cachedScaling = Vector3.One;

            InternalShape = new Sphere(Radius);

            DebugPrimitiveMatrix = Matrix.Scaling(2 * Radius * DebugScaling);
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            return GeometricPrimitive.Sphere.New(device).ToMeshDraw();
        }

        internal override void CreateAndAddCollidableDescription(
            BepuPhysicsComponent physicsComponent,
            BepuSimulation xenkoSimulation,
            out TypedIndex shapeTypeIndex,
            out CollidableDescription collidableDescription)
        {
            CreateAndAddCollidableDescription<Sphere>(
                physicsComponent, xenkoSimulation, out shapeTypeIndex, out collidableDescription);
        }
    }
}
