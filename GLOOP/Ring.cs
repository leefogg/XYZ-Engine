using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public class Ring<T>
    {
        private T[] Items { get; }
        private int CurrentIndex;

        private int NextIndex => (CurrentIndex + 1) & (Items.Length - 1);

        public T Current => Items[CurrentIndex];
        public T Next
        {
            get
            {
                MoveNext();
                return Current;
            }
        }

        public Ring(PowerOfTwo size, Func<int, T> factory)
        {
            Items = new T[(int)size];
            for (int i = 0; i < (int)size; i++)
                Items[i] = factory(i);
        }

        public void MoveNext() => CurrentIndex = NextIndex;
        public T Peek() => Items[NextIndex];
    }
}
