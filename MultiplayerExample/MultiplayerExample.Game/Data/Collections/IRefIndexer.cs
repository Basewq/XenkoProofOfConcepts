namespace MultiplayerExample.Data.Collections
{
    public interface IRefIndexer<T>
    {
        ref T this[int index] { get; }
        int Count { get; }
    }
}
