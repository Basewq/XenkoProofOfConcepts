using System;
using System.Runtime.CompilerServices;
using Stride.Core.Mathematics;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    public struct BepuContactPoint : IEquatable<BepuContactPoint>
    {
        public BepuPhysicsComponent ColliderA;
        public BepuPhysicsComponent ColliderB;
        public float Distance;
        public Vector3 Normal;
        public Vector3 PositionOnA;
        public Vector3 PositionOnB;

        public bool Equals(BepuContactPoint other)
        {
            return EqualsRef(in this, in other);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return obj is BepuContactPoint other && EqualsRef(in this, in other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ColliderA.GetHashCode();
                hashCode = (hashCode * 397) ^ ColliderB.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(in BepuContactPoint left, in BepuContactPoint right)
        {
            return EqualsRef(in left, in right);
        }

        public static bool operator !=(in BepuContactPoint left, in BepuContactPoint right)
        {
            return EqualsRef(in left, in right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool EqualsRef(in BepuContactPoint left, in BepuContactPoint right)
        {
            return (left.ColliderA == right.ColliderA && left.ColliderB == right.ColliderB)
                || (left.ColliderA == right.ColliderB && left.ColliderB == right.ColliderA);
        }
    }
}
