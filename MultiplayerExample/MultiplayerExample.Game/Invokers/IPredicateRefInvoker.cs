using System;

namespace MultiplayerExample.Invokers
{
    /// <remarks>
    /// This is used as an alternative to <see cref="Predicate{T}"/> when
    /// traversing arrays of structs to avoid copying structs to memory.
    /// Create a 'readonly struct' that implements this interface to create a light-weight closure object.
    /// </remarks>
    public interface IPredicateRefInvoker<T>
    {
        bool Invoke(in T obj);
    }

    ////public readonly struct SimplePredicateMatcher<T> : IPredicateRefInvoker<T>
    ////    where T : IEquatable<T>
    ////{
    ////    public SimplePredicateMatcher(T valueToMatch)
    ////    {
    ////        _valueToMatch = valueToMatch;
    ////    }

    ////    public readonly T _valueToMatch;
    ////    public bool Invoke(in T obj)
    ////    {
    ////        return _valueToMatch.Equals(obj);
    ////    }
    ////}

    ////public readonly struct EqualityPredicateMatcher<T> : IPredicateRefInvoker<T>
    ////{
    ////    public EqualityPredicateMatcher(T valueToMatch, Func<T, T, bool> equalityFunc)
    ////    {
    ////        _valueToMatch = valueToMatch;
    ////        _equalityFunc = equalityFunc;
    ////    }

    ////    private readonly T _valueToMatch;
    ////    private readonly Func<T, T, bool> _equalityFunc;
    ////    public bool Invoke(in T obj)
    ////    {
    ////        return _equalityFunc(_valueToMatch, obj);
    ////    }
    ////}
}
