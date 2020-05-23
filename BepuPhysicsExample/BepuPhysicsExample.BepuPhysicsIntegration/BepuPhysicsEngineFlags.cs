using System;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    [Flags]
    public enum BepuPhysicsEngineFlags
    {
        None = 0x0,

        CollisionsOnly = 0x1,

        SoftBodySupport = 0x2,

        MultiThreaded = 0x4,

        UseHardwareWhenPossible = 0x8,

        ContinuousCollisionDetection = 0x10,
    }
}
