using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GLOOP.Util.Structures
{
    public class FastList<T>
    {
        public T[] Elements { get; private set; }
        private int head;
        public int Count
        {
            get => head;
            set
            {
                EnsureCapacity(value + 1);
                head = value;
            }
        }

        public FastList(int initialSize)
        {
            Elements = new T[initialSize];
            Count = 0;
        }

        public int Add(T element)
        {
            Elements[Count++] = element;
            return Count;
        }

        public int AddRange(IEnumerable<T> elements)
        {
            foreach (var element in elements)
                Add(element);
            return Count;
        }

        public int AddRange(Span<T> elements)
        {
            EnsureCapacity(Count + elements.Length);
            Elements.CopyTo(Elements, Count);
            return Count;
        }

        private void EnsureCapacity(int length)
        {
            if (length >= Elements.Length)
                Resize(length);
        }

        private void Resize(int requestedLength)
        {
            var newLength = Count;
            while (newLength < requestedLength)
                newLength *= 2;

            Debug.Fail("Resizing array");

            var newElements = new T[newLength];
            Array.Copy(Elements, newElements, Elements.Length);
            Elements = newElements;
        }

        public void Clear() => head = 0;

        public Span<T> AsSpan() => Elements.AsSpan(Count);
        public Span<T> AsSpan(int count) => Elements.AsSpan(0, count);
        public Span<T> AsSpan(int start, int length) => Elements.AsSpan(start, length);
    }
}
