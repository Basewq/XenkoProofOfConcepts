using System;
using System.Buffers;

namespace MultiplayerExample.Utilities
{
    static class ArrayPoolExt
    {
        public static DisposableArrayPool<T> RentTemp<T>(this ArrayPool<T> pool, int minimumLength)
        {
            return new DisposableArrayPool<T>(pool, minimumLength);
        }

        public readonly struct DisposableArrayPool<T> : IDisposable
        {
            private readonly ArrayPool<T> _pool;

            public readonly T[] Array;

            public DisposableArrayPool(ArrayPool<T> pool, int minimumLength)
            {
                _pool = pool;
                Array = pool.Rent(minimumLength);
            }

            public void Dispose()
            {
                _pool.Return(Array);
            }
        }
    }
}
