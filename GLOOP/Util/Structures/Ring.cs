using GLOOP.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Util.Structures
{
    public class Ring<T> : IEnumerable<T>
    {
        private readonly T[] Items;
        private int CurrentIndex;

        private int NextIndex => CurrentIndex + 1 & Items.Length - 1;

        public int Count => Items.Length;

        public T Current => Items[CurrentIndex];
        public T Next
        {
            get
            {
                MoveNext();
                return Current;
            }
        }

        public Ring(PowerOfTwo size, Func<int, T> factory = null)
        {
            var intitalizer = factory ?? (i => default);
            Items = new T[(int)size];
            for (int i = 0; i < (int)size; i++)
                Items[i] = intitalizer(i);
        }

        public void MoveNext() => CurrentIndex = NextIndex;
        public T Peek() => Items[NextIndex];

        public void SetAndMove(T item)
        {
            Set(item);
            MoveNext();
        }
        public void Set(T item)
        {
            Items[CurrentIndex] = item;
        }

        public IEnumerator<T> GetEnumerator() => new RingEnumerator<T>(Items, CurrentIndex);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private class RingEnumerator<T> : IEnumerator<T>
        {
            private int start, current;
            private T[] Items;

            public RingEnumerator(T[] items, int startIndex)
            {
                Items = items;
                start = startIndex;
                current = start + 1;
                ensureIndex();
            }

            public T Current
            {
                get
                {
                    var item = Items[current];
                    current++;
                    ensureIndex();
                    return item;
                }
            }

            private void ensureIndex()
            {
                current &= Items.Length - 1;
            }

            object IEnumerator.Current => Current;

            public void Dispose() { }
            public bool MoveNext() => current != start;
            public void Reset() => current = start;
        }
    }
}
