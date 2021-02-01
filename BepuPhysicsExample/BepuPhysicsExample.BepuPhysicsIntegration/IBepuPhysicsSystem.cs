using Stride.Games;
using Stride.Physics;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    public interface IBepuPhysicsSystem : IGameSystemBase
    {
        BepuSimulation Create(BepuPhysicsProcessor processor, BepuPhysicsEngineFlags flags = BepuPhysicsEngineFlags.None);
        void Release(BepuPhysicsProcessor processor);
    }
}
