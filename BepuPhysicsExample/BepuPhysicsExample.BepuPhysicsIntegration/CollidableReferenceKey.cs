using BepuPhysics.Collidables;
using System;
using System.Runtime.CompilerServices;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    public struct CollidableReferenceKey : IEquatable<CollidableReferenceKey>
    {
        public static readonly CollidableReferenceKey NotSet = default;

        private CollidableReference collidableReference;

        public CollidableReferenceKey(in CollidableReference collidableReference)
        {
            this.collidableReference = collidableReference;
        }

        public CollidableReferenceKey(CollidableMobility mobility, int handle)
        {
            collidableReference = new CollidableReference(mobility, handle);
        }

        public bool IsSet => this != NotSet;

        public int HandleId => collidableReference.Handle;

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return obj is CollidableReferenceKey other && EqualsRef(this, other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(CollidableReferenceKey other)
        {
            return EqualsRef(this, other);
        }

        private static bool EqualsRef(in CollidableReferenceKey left, in CollidableReferenceKey right)
        {
            return left.collidableReference.Packed == right.collidableReference.Packed;
        }

        public override int GetHashCode()
        {
            return collidableReference.Packed.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in CollidableReferenceKey left, in CollidableReferenceKey right)
        {
            return EqualsRef(left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in CollidableReferenceKey left, in CollidableReferenceKey right)
        {
            return !EqualsRef(left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator CollidableReference(CollidableReferenceKey key)
        {
            return key.collidableReference;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator CollidableReferenceKey(CollidableReference collidableReference)
        {
            return new CollidableReferenceKey(collidableReference);
        }
    }
}
