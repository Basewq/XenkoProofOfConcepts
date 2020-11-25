using MultiplayerExample.Core;
using MultiplayerExample.Data;
using MultiplayerExample.Data.Collections;
using MultiplayerExample.Invokers;
using System;

namespace MultiplayerExample.Network.SnapshotStores
{
    class SnapshotStore<T> : IObjectPoolItem where T : struct, ISnapshotData
    {
        private readonly RingBuffer<T> _snapshotRingBuffer;

        public bool IsEmpty => _snapshotRingBuffer.IsEmpty;

        public bool IsFull => _snapshotRingBuffer.Capacity == _snapshotRingBuffer.Count;

        public int Capacity => _snapshotRingBuffer.Capacity;

        public int Count => _snapshotRingBuffer.Count;

        public SnapshotStore(int snapshotBufferSize)
        {
            _snapshotRingBuffer = new RingBuffer<T>(snapshotBufferSize);
        }

        public FindSnapshotResult TryFindSnapshot(SimulationTickNumber simTickNumber)
        {
            var matcher = new MatchEqualToSimNumber(simTickNumber);
            bool isFound = _snapshotRingBuffer.TryFindLastIndex(matcher, out int index);
            if (isFound)
            {
                return new FindSnapshotResult(_snapshotRingBuffer, index);
            }
            else
            {
                return FindSnapshotResult.NotFound;
            }
        }

        public FindSnapshotResult TryFindSnapshotClosestEqualOrGreaterThan(SimulationTickNumber simTickNumber)
        {
            bool isFound = false;
            var closestSimTickNo = new SimulationTickNumber(long.MaxValue);
            int closestIndex = -1;
            for (int i = 0; i < _snapshotRingBuffer.Count; i++)
            {
                var curSimTickNo = _snapshotRingBuffer[i].SimulationTickNumber;
                if (simTickNumber == curSimTickNo)
                {
                    return new FindSnapshotResult(_snapshotRingBuffer, i);
                }
                else if (curSimTickNo > simTickNumber && curSimTickNo < closestSimTickNo)
                {
                    isFound = true;
                    closestSimTickNo = curSimTickNo;
                    closestIndex = i;
                }
            }
            if (isFound)
            {
                return new FindSnapshotResult(_snapshotRingBuffer, closestIndex);
            }
            else
            {
                return FindSnapshotResult.NotFound;
            }
        }

        public FindSnapshotResult TryFindSnapshotClosestEqualOrLessThan(SimulationTickNumber simTickNumber)
        {
            bool isFound = false;
            var closestSimTickNo = new SimulationTickNumber(0);
            int closestIndex = -1;
            for (int i = 0; i < _snapshotRingBuffer.Count; i++)
            {
                var curSimTickNo = _snapshotRingBuffer[i].SimulationTickNumber;
                if (simTickNumber == curSimTickNo)
                {
                    return new FindSnapshotResult(_snapshotRingBuffer, i);
                }
                else if (curSimTickNo < simTickNumber && curSimTickNo > closestSimTickNo)
                {
                    isFound = true;
                    closestSimTickNo = curSimTickNo;
                    closestIndex = i;
                }
            }
            if (isFound)
            {
                return new FindSnapshotResult(_snapshotRingBuffer, closestIndex);
            }
            else
            {
                return FindSnapshotResult.NotFound;
            }
        }

        public FindSnapshotResult TryFindSnapshotClosestAnyDirection(SimulationTickNumber simTickNumber)
        {
            bool isFound = false;
            var closestSimTickNoGreater = new SimulationTickNumber(long.MaxValue);
            int closestIndexGreater = -1;
            var closestSimTickNoLesser = new SimulationTickNumber(0);
            int closestIndexLesser = -1;
            for (int i = 0; i < _snapshotRingBuffer.Count; i++)
            {
                var curSimTickNo = _snapshotRingBuffer[i].SimulationTickNumber;
                if (simTickNumber == curSimTickNo)
                {
                    return new FindSnapshotResult(_snapshotRingBuffer, i);
                }
                if (curSimTickNo > simTickNumber && curSimTickNo < closestSimTickNoGreater)
                {
                    isFound = true;
                    closestSimTickNoGreater = curSimTickNo;
                    closestIndexGreater = i;
                }
                if (curSimTickNo < simTickNumber && curSimTickNo > closestSimTickNoLesser)
                {
                    isFound = true;
                    closestSimTickNoLesser = curSimTickNo;
                    closestIndexLesser = i;
                }
            }
            if (isFound)
            {
                if ((closestSimTickNoGreater - simTickNumber) < (simTickNumber - closestSimTickNoLesser))
                {
                    return new FindSnapshotResult(_snapshotRingBuffer, closestIndexGreater);
                }
                else
                {
                    return new FindSnapshotResult(_snapshotRingBuffer, closestIndexLesser);
                }
            }
            else
            {
                return FindSnapshotResult.NotFound;
            }
        }

        public void Clear() => _snapshotRingBuffer.Clear();

        public ref T GetOrCreate(SimulationTickNumber simTickNumber)
        {
            var findResult = TryFindSnapshot(simTickNumber);
            if (findResult.IsFound)
            {
                return ref findResult.Result;
            }
            else
            {
                _snapshotRingBuffer.Add(default);
                return ref _snapshotRingBuffer.GetLatest();
            }
        }

        public ref T GetOrCreate(SimulationTickNumber simTickNumber, out bool wasCreated)
        {
            var findResult = TryFindSnapshot(simTickNumber);
            if (findResult.IsFound)
            {
                wasCreated = false;
                return ref findResult.Result;
            }
            else
            {
                wasCreated = true;
                _snapshotRingBuffer.Add(default);
                return ref _snapshotRingBuffer.GetLatest();
            }
        }

        public ref T Create()
        {
            _snapshotRingBuffer.Add(default);
            return ref _snapshotRingBuffer.GetLatest();
        }

        /// <summary>
        /// Gets the most recently added item.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the store is empty.</exception>
        /// <returns></returns>
        public ref T GetLatest()
        {
            return ref _snapshotRingBuffer.GetLatest();
        }

        public ref T GetPrevious(int indexOffset)
        {
            return ref _snapshotRingBuffer.GetPrevious(indexOffset);
        }

        /// <summary>
        /// Gets the oldest added item.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the store is empty.</exception>
        public ref T GetLast()
        {
            return ref _snapshotRingBuffer.GetLast();
        }

        public readonly struct FindSnapshotResult
        {
            public static FindSnapshotResult NotFound { get; } = new FindSnapshotResult(snapshotData: null, index: -1, isFound: false);

            private readonly RingBuffer<T> _snapshotData;
            private readonly int _index;

            public readonly bool IsFound;

            public ref T Result => ref _snapshotData[_index];

            private FindSnapshotResult(RingBuffer<T> snapshotData, int index, bool isFound)
            {
                _snapshotData = snapshotData;
                _index = index;
                IsFound = isFound;
            }

            public FindSnapshotResult(RingBuffer<T> snapshotData, int index)
            {
                _snapshotData = snapshotData;
                _index = index;
                IsFound = true;
            }
        }

        private readonly struct MatchEqualToSimNumber : IPredicateRefInvoker<T>
        {
            private readonly SimulationTickNumber _simTickNumber;

            public MatchEqualToSimNumber(SimulationTickNumber simTickNumber)
            {
                _simTickNumber = simTickNumber;
            }

            public bool Invoke(in T obj) => _simTickNumber == obj.SimulationTickNumber;
        }
    }
}
