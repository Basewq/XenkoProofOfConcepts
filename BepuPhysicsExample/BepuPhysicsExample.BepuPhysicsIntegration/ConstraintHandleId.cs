using System;
using System.Runtime.CompilerServices;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    public struct ConstraintHandleId : IEquatable<ConstraintHandleId>
    {
        public static readonly ConstraintHandleId NotSet = new ConstraintHandleId(-1);

        public ConstraintHandleId(int constraintHandleId)
        {
            HandleId = constraintHandleId;
        }

        public bool IsSet => this != NotSet;

        public int HandleId { get; }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return obj is ConstraintHandleId other && EqualsRef(this, other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ConstraintHandleId other)
        {
            return EqualsRef(this, other);
        }

        private static bool EqualsRef(in ConstraintHandleId left, in ConstraintHandleId right)
        {
            return left.HandleId == right.HandleId;
        }

        public override int GetHashCode()
        {
            return HandleId.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in ConstraintHandleId left, in ConstraintHandleId right)
        {
            return EqualsRef(left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in ConstraintHandleId left, in ConstraintHandleId right)
        {
            return !EqualsRef(left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(ConstraintHandleId key)
        {
            return key.HandleId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ConstraintHandleId(int constraintHandleId)
        {
            return new ConstraintHandleId(constraintHandleId);
        }
    }
}
