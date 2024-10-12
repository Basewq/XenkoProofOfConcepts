using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SceneEditorExtensionExample;

public static class DisposableExtensions
{
    public static IDisposable Empty { get; } = new EmptyDisposable();

    public static void DisposeAndNull<T>(ref T disposable) where T : class, IDisposable
    {
        disposable?.Dispose();
        disposable = null;
    }

    /// <summary>
    /// Iterate through the list and call <see cref="IDisposable.Dispose"/> on all items,
    /// and clear the list if <paramref name="clearList"/> is <c>true</c>.
    /// </summary>
    public static void DisposeAll<T>(this List<T> disposableList, bool clearList = true) where T : class, IDisposable
    {
        foreach (var disp in CollectionsMarshal.AsSpan(disposableList))
        {
            disp.Dispose();
        }
        if (clearList)
        {
            disposableList.Clear();
        }
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
