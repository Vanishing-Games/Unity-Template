using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    namespace Core
    {
        public class PriorityQueue<TElement, TPriority>
            : IEnumerable<TElement>
        {
            private readonly List<(TElement Element, TPriority Priority)> mHeap;
            private readonly IComparer<TPriority> mComparer;
            private readonly object mSyncRoot = new();

            public int Count
            {
                get
                {
                    lock (mSyncRoot)
                        return mHeap.Count;
                }
            }

            public PriorityQueue()
                : this(null) { }

            public PriorityQueue(IComparer<TPriority> comparer)
            {
                mHeap = new List<(TElement, TPriority)>();
                mComparer = comparer ?? Comparer<TPriority>.Default;
            }

            public void Enqueue(TElement element, TPriority priority)
            {
                lock (mSyncRoot)
                {
                    mHeap.Add((element, priority));
                    HeapifyUp(mHeap.Count - 1);
                }
            }

            public bool TryDequeue(out TElement element, out TPriority priority)
            {
                lock (mSyncRoot)
                {
                    if (mHeap.Count == 0)
                    {
                        element = default;
                        priority = default;
                        return false;
                    }

                    (element, priority) = mHeap[0];
                    var last = mHeap[^1];
                    mHeap.RemoveAt(mHeap.Count - 1);

                    if (mHeap.Count > 0)
                    {
                        mHeap[0] = last;
                        HeapifyDown(0);
                    }

                    return true;
                }
            }

            public bool TryPeek(out TElement element, out TPriority priority)
            {
                lock (mSyncRoot)
                {
                    if (mHeap.Count == 0)
                    {
                        element = default;
                        priority = default;
                        return false;
                    }

                    (element, priority) = mHeap[0];
                    return true;
                }
            }

            public void Clear()
            {
                lock (mSyncRoot)
                {
                    mHeap.Clear();
                }
            }

            private void HeapifyUp(int index)
            {
                while (index > 0)
                {
                    int parent = (index - 1) / 2;
                    if (mComparer.Compare(mHeap[index].Priority, mHeap[parent].Priority) >= 0)
                        break;

                    Swap(index, parent);
                    index = parent;
                }
            }

            private void HeapifyDown(int index)
            {
                int count = mHeap.Count;
                while (true)
                {
                    int left = (index * 2) + 1;
                    int right = index * 2 + 2;
                    int smallest = index;

                    if (
                        left < count
                        && mComparer.Compare(mHeap[left].Priority, mHeap[smallest].Priority) < 0
                    )
                    {
                        smallest = left;
                    }

                    if (
                        right < count
                        && mComparer.Compare(mHeap[right].Priority, mHeap[smallest].Priority) < 0
                    )
                    {
                        smallest = right;
                    }

                    if (smallest == index)
                        break;

                    Swap(index, smallest);
                    index = smallest;
                }
            }

            private void Swap(int i, int j)
            {
                (mHeap[j], mHeap[i]) = (mHeap[i], mHeap[j]);
            }

            /// <summary>
            /// Do not modify the queue while iterating.
            /// </summary>
            public IEnumerator<TElement> GetEnumerator() =>
                mHeap.OrderBy(x => x.Priority).Select(x => x.Element).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
