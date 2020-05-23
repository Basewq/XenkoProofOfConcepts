using Microsoft.Extensions.ObjectPool;
using System;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    static class ObjectPoolExtensions
    {
        public static ObjectPoolItemReturner<T> GetAndReturn<T>(this ObjectPool<T> pool) where T : class
        {
            var item = pool.Get();
            return new ObjectPoolItemReturner<T>(pool, item);
        }

        public struct ObjectPoolItemReturner<T> : IDisposable where T : class
        {
            private readonly ObjectPool<T> objectPool;
            public readonly T Item;

            public ObjectPoolItemReturner(ObjectPool<T> objectPool, T item)
            {
                this.objectPool = objectPool;
                this.Item = item;
            }

            public void Dispose()
            {
                objectPool.Return(Item);
            }

            public static implicit operator T(ObjectPoolItemReturner<T> poolItemReturner)
            {
                return poolItemReturner.Item;
            }
        }
    }
}
