using BepuPhysicsExample.BepuPhysicsIntegration;
using Stride.Core.Assets;

namespace BepuPhysicsExample.GameStudioExt.Physics
{
    public class BepuColliderShapeBoxFactory : AssetFactory<BepuColliderShapeAsset>
    {
        public static BepuColliderShapeAsset Create()
        {
            return new BepuColliderShapeAsset { ColliderShapes = { new BepuBoxColliderShapeDesc() } };
        }

        public override BepuColliderShapeAsset New()
        {
            return Create();
        }
    }

    public class BepuColliderShapeCapsuleFactory : AssetFactory<BepuColliderShapeAsset>
    {
        public static BepuColliderShapeAsset Create()
        {
            return new BepuColliderShapeAsset { ColliderShapes = { new BepuCapsuleColliderShapeDesc() } };
        }

        public override BepuColliderShapeAsset New()
        {
            return Create();
        }
    }

    public class BepuColliderShapeConvexHullFactory : AssetFactory<BepuColliderShapeAsset>
    {
        public static BepuColliderShapeAsset Create()
        {
            return new BepuColliderShapeAsset { ColliderShapes = { new BepuConvexHullColliderShapeDesc() } };
        }

        public override BepuColliderShapeAsset New()
        {
            return Create();
        }
    }

    public class BepuColliderShapeCylinderFactory : AssetFactory<BepuColliderShapeAsset>
    {
        public static BepuColliderShapeAsset Create()
        {
            return new BepuColliderShapeAsset { ColliderShapes = { new BepuCylinderColliderShapeDesc() } };
        }

        public override BepuColliderShapeAsset New()
        {
            return Create();
        }
    }

    //public class BepuColliderShapeConeFactory : AssetFactory<BepuColliderShapeAsset>
    //{
    //    public static BepuColliderShapeAsset Create()
    //    {
    //        return new BepuColliderShapeAsset { ColliderShapes = { new BepuConeColliderShapeDesc() } };
    //    }

    //    public override BepuColliderShapeAsset New()
    //    {
    //        return Create();
    //    }
    //}

    public class BepuColliderShapePlaneFactory : AssetFactory<BepuColliderShapeAsset>
    {
        public static BepuColliderShapeAsset Create()
        {
            return new BepuColliderShapeAsset { ColliderShapes = { new BepuStaticPlaneColliderShapeDesc() } };
        }

        public override BepuColliderShapeAsset New()
        {
            return Create();
        }
    }

    public class BepuColliderShapeSphereFactory : AssetFactory<BepuColliderShapeAsset>
    {
        public static BepuColliderShapeAsset Create()
        {
            return new BepuColliderShapeAsset { ColliderShapes = { new BepuSphereColliderShapeDesc() } };
        }

        public override BepuColliderShapeAsset New()
        {
            return Create();
        }
    }
}
