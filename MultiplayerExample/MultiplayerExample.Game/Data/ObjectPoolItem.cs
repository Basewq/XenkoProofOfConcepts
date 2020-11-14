using System;

namespace MultiplayerExample.Data
{
    public interface IObjectPoolItem
    {
        void Clear();
    }

    public static class ObjectPoolItemCreator<T>
        where T : IObjectPoolItem, new()
    {
        public static Func<T> Default { get; } = () => new T();
    }

    public static class ObjectPoolItemClearer<T>
        where T : IObjectPoolItem
    {
        public static Action<T> Default { get; } = (T obj) => obj.Clear();
    }
}
