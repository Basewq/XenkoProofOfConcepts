namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    public interface IBepuColliderShapeDesc
    {
        bool Match(object obj);
        BepuColliderShape CreateShape(BepuUtilities.Memory.BufferPool bufferPool);
    }

    public interface IBepuAssetColliderShapeDesc : IBepuColliderShapeDesc
    {
    }

    public interface IBepuInlineColliderShapeDesc : IBepuAssetColliderShapeDesc
    {
    }
}
