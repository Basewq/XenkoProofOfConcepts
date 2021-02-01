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
    public class BepuStaticPlaneColliderShape : BepuColliderShape
    {
        private readonly BepuUtilities.Memory.BufferPool bufferPool;

        public readonly Vector3 Normal;
        public readonly float Offset;

        /// <summary>
        /// Initializes a new instance of the <see cref="BepuStaticPlaneColliderShape"/> class.
        /// A static plane that is solid to infinity on one side.
        /// Several of these can be used to confine a convex space in a manner that completely prevents tunneling to the outside.
        /// The plane itself is specified with a normal and distance as is standard in mathematics.
        /// </summary>
        /// <param name="normal">The normal.</param>
        /// <param name="offset">The offset.</param>
        public BepuStaticPlaneColliderShape(Vector3 normal, float offset, BepuUtilities.Memory.BufferPool bufferPool)
        {
            this.bufferPool = bufferPool;

            Type = ColliderShapeTypes.StaticPlane;
            Normal = normal;
            Offset = offset;

            cachedScaling = Vector3.One;

            Matrix rotationMatrix;
            var oY = Vector3.Normalize(Normal);
            var oZ = Vector3.Cross(Vector3.UnitX, oY);
            if (oZ.Length() > MathUtil.ZeroTolerance)
            {
                oZ.Normalize();
                var oX = Vector3.Cross(oY, oZ);
                rotationMatrix = new Matrix(
                    oX.X, oX.Y, oX.Z, 0,
                    oY.X, oY.Y, oY.Z, 0,
                    oZ.X, oZ.Y, oZ.Z, 0,
                    0, 0, 0, 1);
            }
            else
            {
                var s = Math.Sign(oY.X);
                rotationMatrix = new Matrix(
                    0, s, 0, 0,
                    s, 0, 0, 0,
                    0, 0, 1, 0,
                    0, 0, 0, 1);
            }

            var transformMatrix = Matrix.Translation(Offset * Vector3.UnitY) * rotationMatrix;

            // Infinite plane doesn't exist in Bepu!
            CreatePlane(cachedScaling, transformMatrix, bufferPool, out var planeMesh);

            InternalShape = planeMesh;

            DebugPrimitiveMatrix = transformMatrix;
        }

        private static void CreatePlane(
            in Vector3 scaling, in Matrix transformMatrix, BepuUtilities.Memory.BufferPool bufferPool,
            out BepuPhysics.Collidables.Mesh mesh)
        {
            // Create a quad
            const int TriangleCount = 2;    // Two triangles make a quad
            bufferPool.Take<Triangle>(TriangleCount, out var triangles);

            //const float LowerBound = float.MinValue;
            //const float UpperBound = float.MaxValue;
            const float LowerBound = -10000f;   // Can't use float.Min/Max, it crashes Bepu...
            const float UpperBound = 10000f;

            ref var triangle0 = ref triangles[0];
            var v00 = ((Vector3)Vector3.Transform(new Vector3(LowerBound, 0, LowerBound), transformMatrix)).ToNumericsVector3();
            var v01 = ((Vector3)Vector3.Transform(new Vector3(LowerBound, 0, UpperBound), transformMatrix)).ToNumericsVector3();
            var v10 = ((Vector3)Vector3.Transform(new Vector3(UpperBound, 0, LowerBound), transformMatrix)).ToNumericsVector3();
            var v11 = ((Vector3)Vector3.Transform(new Vector3(UpperBound, 0, UpperBound), transformMatrix)).ToNumericsVector3();
            triangle0.A = v00;
            triangle0.B = v10;
            triangle0.C = v01;
            ref var triangle1 = ref triangles[1];
            triangle1.A = v10;
            triangle1.B = v11;
            triangle1.C = v01;

            mesh = new BepuPhysics.Collidables.Mesh(triangles, scaling.ToNumericsVector3(), bufferPool);
        }

        public override void Dispose()
        {
            var shape = (BepuPhysics.Collidables.Mesh)InternalShape;
            shape.Dispose(bufferPool);

            base.Dispose();
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            return GeometricPrimitive.Plane.New(device, 1000, 1000, 100, 100, normalDirection: NormalDirection.UpY).ToMeshDraw();
        }

        internal override void CreateAndAddCollidableDescription(
            BepuPhysicsComponent physicsComponent,
            BepuSimulation xenkoSimulation,
            out TypedIndex shapeTypeIndex,
            out CollidableDescription collidableDescription)
        {
            CreateAndAddCollidableDescription<BepuPhysics.Collidables.Mesh>(
                physicsComponent, xenkoSimulation, out shapeTypeIndex, out collidableDescription);
        }
    }
}
