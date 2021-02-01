using System;
using Stride.Core.Mathematics;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Physics;
using Stride.Rendering;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    //public class BepuConeColliderShape : BepuColliderShape
    //{
    //    public readonly float Height;
    //    public readonly float Radius;
    //    //public readonly ShapeOrientation Orientation;

    //    /// <summary>
    //    /// Initializes a new instance of the <see cref="BepuConeColliderShape"/> class.
    //    /// </summary>
    //    /// <param name="radius">The radius of the cone</param>
    //    /// <param name="height">The height of the cone</param>
    //    public BepuConeColliderShape(float height, float radius)
    //    {
    //        Type = ColliderShapeTypes.Cone;
    //        //Is2D = false; //always false for cone
    //        Height = height;
    //        Radius = radius;
    //        //Orientation = orientationParam;

    //        cachedScaling = Vector3.One;

    //        switch (Orientation)
    //        {
    //            case ShapeOrientation.UpX:
    //                InternalShape = new BepuPhysics.Collidables.ConeX(Radius, Height)
    //                {
    //                    LocalScaling = cachedScaling,
    //                };
    //                rotation = Matrix.RotationZ((float)Math.PI / 2.0f);
    //                break;
    //            case ShapeOrientation.UpY:
    //                InternalShape = new BepuPhysics.Collidables.ConeY(Radius, Height);
    //                rotation = Matrix.Identity;
    //                break;
    //            case ShapeOrientation.UpZ:
    //                InternalShape = new BepuPhysics.Collidables.ConeZ(Radius, Height)
    //                {
    //                    LocalScaling = cachedScaling,
    //                };
    //                rotation = Matrix.RotationX((float)Math.PI / 2.0f);
    //                break;
    //            default:
    //                throw new ArgumentOutOfRangeException(nameof(Orientation));
    //        }

    //        DebugPrimitiveMatrix = Matrix.Scaling(new Vector3(Radius * 2, Height, Radius * 2) * DebugScaling);// * rotation;
    //    }

    //    public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
    //    {
    //        return GeometricPrimitive.Cone.New(device).ToMeshDraw();
    //    }
    //}
}
