using MultiplayerExample.Invokers;
using MultiplayerExample.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace MultiplayerExample.Data.Collections
{
    public class RingBuffer<T> : IEnumerable<T>, IRefIndexer<T>
    {
        private T[] _buffer;
        private int _startIndex = -1;
        private int _count = 0;

        public RingBuffer(int maxCapacity)
        {
            _buffer = new T[maxCapacity];
        }

        /// <param name="index">Index range [0...<see cref="Count"/>), where 0 is the oldest item added.</param>
        public ref T this[int index]
        {
            get
            {
                Debug.Assert(IsIndexInBounds(index), "Index out of range.");
                int bufferIndex = (_startIndex + index).WrapOn(_buffer.Length);
                return ref _buffer[bufferIndex];
            }
        }

        public bool IsEmpty => _count == 0;

        public bool IsFull => _count == _buffer.Length;

        /// <summary>
        /// Gets the number of items in this buffer.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Gets the total number of items in this buffer before cycling over the previous items.
        /// </summary>
        public int Capacity => _buffer.Length;

        public bool IsIndexInBounds(int index)
        {
            bool isInBounds = (0 <= index && index < _count);
            return isInBounds;
        }

        /// <summary>
        /// Adds to the end of the buffer.
        /// </summary>
        /// <remarks>The index the added item will be (<see cref="Count"/> - 1), or just use <see cref="GetLatest"/></remarks>
        public void Add(in T item)
        {
            if (_count >= _buffer.Length)
            {
                // Overwrite first entry
                _buffer[_startIndex] = item;
                _startIndex = (_startIndex + 1).WrapOn(_buffer.Length);
            }
            else
            {
                if (_count == 0)
                {
                    _startIndex = 0;
                }
                _count++;
                int itemIndex = (_startIndex + _count - 1).WrapOn(_buffer.Length);
                _buffer[itemIndex] = item;
            }
        }

        public void Clear()
        {
            _startIndex = 0;
            _count = 0;
            Array.Clear(_buffer, 0, _buffer.Length);
        }

        /// <summary>
        /// Gets the most recently added item.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if buffer is empty.</exception>
        /// <returns></returns>
        public ref T GetLatest()
        {
            if (_count <= 0)
            {
                throw new InvalidOperationException("Buffer is empty.");
            }
            Debug.Assert(_startIndex >= 0, $"Buffer is not empty but {nameof(_startIndex)} is not set.");

            int index = (_startIndex + _count - 1).WrapOn(_buffer.Length);
            return ref _buffer[index];
        }

        /// <summary>
        /// Gets the oldest added item.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if buffer is empty.</exception>
        public ref T GetLast()
        {
            if (_count <= 0)
            {
                throw new InvalidOperationException("Buffer is empty.");
            }
            Debug.Assert(_startIndex >= 0, $"Buffer is not empty but {nameof(_startIndex)} is not set.");

            return ref _buffer[_startIndex];
        }

        /// <param name="indexOffset">Zero is the current item (most recently added item). Range is [0...(<see cref="Count"/> - 1)]</param>
        public ref T GetPrevious(int indexOffset)
        {
            if (_count <= 0)
            {
                throw new InvalidOperationException("Buffer is empty.");
            }
            Debug.Assert(_startIndex >= 0, $"Buffer is not empty but {nameof(_startIndex)} is not set.");
            Debug.Assert(0 <= indexOffset && indexOffset < _count, "Index out of range.");

            int index = (_startIndex + _count - 1 - indexOffset).WrapOn(_buffer.Length);
            return ref _buffer[index];
        }

        /// <summary>
        /// Finds the index of the item starting from the most recently added item to the oldest item,
        /// or -1 if no match is found.
        /// </summary>
        public bool TryFindLastIndex<TPredicateInvoker>(in TPredicateInvoker match, out int index)
            where TPredicateInvoker : IPredicateRefInvoker<T>
        {
            for (int i = 0; i < _count; i++)
            {
                int idx = (_startIndex + _count - 1 - i).WrapOn(_buffer.Length);
                ref readonly var item = ref this[idx];
                bool isMatch = match.Invoke(item);
                if (isMatch)
                {
                    index = idx;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
            {
                int index = (_startIndex + i).WrapOn(_buffer.Length);
                yield return _buffer[index];        // Use struct iterator instead of yield return?
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
