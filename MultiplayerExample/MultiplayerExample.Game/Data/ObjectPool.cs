using System;
using System.Collections.Generic;

namespace MultiplayerExample.Data
{
    /// <summary>
    /// Thread-safe object pool.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObjectPool<T>
    {
        private object _syncRoot = new object();
        private List<T> _pool;
        private Func<T> _objectCreatorFunc;
        private Action<T> _onObjectPutAction;

        /// <summary>
        /// Number of items currently in the pool.
        /// </summary>
        public int Count => _pool.Count;

        public ObjectPool(int initialCapacity, Func<T> objectCreatorFunc, Action<T>  onObjectPutAction = null, bool createImmediately = false)
        {
            if (initialCapacity <= 0)
            {
                throw new ArgumentException("Capacity must be a positive non-zero value.", nameof(initialCapacity));
            }

            _pool = new List<T>(initialCapacity);
            _objectCreatorFunc = objectCreatorFunc
                                    ?? throw new ArgumentNullException(nameof(objectCreatorFunc));
            _onObjectPutAction = onObjectPutAction;

            if (createImmediately)
            {
                lock (_syncRoot)
                {
                    for (int i = 0; i < initialCapacity; i++)
                    {
                        var obj = _objectCreatorFunc();
                        _pool.Add(obj);
                    }
                }
            }
        }

        /// <summary>
        /// Get an object from the pool if available, or creates a new instance of the object.
        /// </summary>
        public T GetObject()
        {
            lock (_syncRoot)
            {
                if (_pool.Count > 0)
                {
                    int index = _pool.Count - 1;
                    var item = _pool[index];
                    _pool.RemoveAt(index);
                    return item;
                }
            }
            return _objectCreatorFunc();
        }

        /// <summary>
        /// Convenience method that gets a disposable struct that returns the object on dispose.
        /// To be used in using statements.
        /// </summary>
        public ObjectPoolReturner BeginGetObject()
        {
            var obj = GetObject();
            var returnable = new ObjectPoolReturner(this, obj);
            return returnable;
        }

        /// <summary>
        /// Puts object in the pool for future use.
        /// </summary>
        public void PutObject(T item)
        {
            _onObjectPutAction?.Invoke(item);
            lock (_syncRoot)
            {
                _pool.Add(item);
            }
        }

        public struct ObjectPoolReturner : IDisposable
        {
            private ObjectPool<T> _objectPool;
            private T _object;

            public ObjectPoolReturner(ObjectPool<T> objectPool, T obj)
            {
                _objectPool = objectPool;
                _object = obj;
            }

            public T Object => _object;

            public void Dispose()
            {
                _objectPool.PutObject(_object);
                _object = default!;
            }
        }
    }
}
