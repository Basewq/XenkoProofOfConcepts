using System.Runtime.InteropServices;
using Xenko.Core.Mathematics;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    /// <summary>
    /// The result of a Physics Raycast or ShapeSweep operation
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct BepuHitResult
    {
        public static readonly BepuHitResult NoHit = default;

        public bool Succeeded;

        /// <summary>
        /// Normal in world space.
        /// </summary>
        public Vector3 Normal;

        /// <summary>
        /// The position in world space where the hit occurred.
        /// </summary>
        public Vector3 Point;

        /// <summary>
        /// Value between 0 and 1.
        /// </summary>
        public float HitFraction;

        /// <summary>
        /// The Collider hit if Succeeded
        /// </summary>
        public BepuPhysicsComponent Collider;
    }
}
