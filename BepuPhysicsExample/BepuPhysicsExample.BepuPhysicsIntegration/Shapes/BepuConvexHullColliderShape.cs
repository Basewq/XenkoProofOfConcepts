using BepuPhysics.Collidables;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Physics;
using Stride.Rendering;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    public class BepuConvexHullColliderShape : BepuColliderShape
    {
        private readonly BepuUtilities.Memory.BufferPool bufferPool;
        private readonly IReadOnlyList<Vector3> pointsList;
        private readonly IReadOnlyList<uint> indicesList;

        public readonly Vector3 HullCenter;

        public BepuConvexHullColliderShape(
            IReadOnlyList<Vector3> points, IReadOnlyList<uint> indices, Vector3 scaling, BepuUtilities.Memory.BufferPool bufferPool)
        {
            Type = ColliderShapeTypes.ConvexHull;
            //Is2D = false;

            cachedScaling = scaling;

            pointsList = points;
            indicesList = indices;

            this.bufferPool = bufferPool;
            bufferPool.Take<System.Numerics.Vector3>(points.Count, out var vertexBuffer);
            for (int i = 0; i < vertexBuffer.Length; i++)
            {
                vertexBuffer[i] = points[i].ToNumericsVector3();
            }
            InternalShape = new ConvexHull(vertexBuffer, bufferPool, out var hullCenter);
            HullCenter = hullCenter.ToXenkoVector3();

            DebugPrimitiveMatrix = Matrix.Scaling(Vector3.One * DebugScaling);
        }

        public IReadOnlyList<Vector3> Points => pointsList;

        public IReadOnlyList<uint> Indices => indicesList;

        public override void Dispose()
        {
            var shape = (ConvexHull)InternalShape;
            shape.Dispose(bufferPool);

            base.Dispose();
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            var verts = new VertexPositionNormalTexture[pointsList.Count];
            for (int i = 0; i < pointsList.Count; i++)
            {
                verts[i].Position = pointsList[i];
                verts[i].TextureCoordinate = Vector2.Zero;
                verts[i].Normal = Vector3.Zero;
            }

            var intIndices = indicesList.Select(x => (int)x).ToArray();

            // Calculate basic normals
            for (int i = 0; i < indicesList.Count; i += 3)
            {
                int i1 = intIndices[i];
                int i2 = intIndices[i + 1];
                int i3 = intIndices[i + 2];
                ref var a = ref verts[i1];
                ref var b = ref verts[i2];
                ref var c = ref verts[i3];
                var n = Vector3.Cross((b.Position - a.Position), (c.Position - a.Position));
                n.Normalize();
                verts[i1].Normal = verts[i2].Normal = verts[i3].Normal = n;
            }

            var meshData = new GeometricMeshData<VertexPositionNormalTexture>(verts, intIndices, isLeftHanded: false);

            return new GeometricPrimitive(device, meshData).ToMeshDraw();
        }

        internal override void CreateAndAddCollidableDescription(
            BepuPhysicsComponent physicsComponent,
            BepuSimulation xenkoSimulation,
            out TypedIndex shapeTypeIndex,
            out CollidableDescription collidableDescription)
        {
            CreateAndAddCollidableDescription<ConvexHull>(
                physicsComponent, xenkoSimulation, out shapeTypeIndex, out collidableDescription);
        }
    }
}
