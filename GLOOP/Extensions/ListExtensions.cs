using OpenTK;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;

namespace GLOOP.Extensions
{
    public static class ListExtensions
    {
        public static IEnumerable<float> GetFloats(this IEnumerable<Vector3> self)
        {
            foreach (var vector in self)
            {
                yield return vector.X;
                yield return vector.Y;
                yield return vector.Z;
            }
        }

        public static IEnumerable<float> GetFloats(this IEnumerable<Vector2> self)
        {
            foreach (var vector in self)
            {
                yield return vector.X;
                yield return vector.Y;
            }
        }

        public static int SizeInBytes<T>(this T[] self) => Marshal.SizeOf<T>() * self.Length;

        public static void RemoveRange<T>(this IList<T> self, IEnumerable<T> items)
        {
            foreach (var item in items)
                self.Remove(item);
        }

        public static string ToHumanList<T>(this IEnumerable<T> self) => new StringBuilder().AppendJoin(", ", self).ToString();

        public static IEnumerable<IEnumerable<T>> Chunk<T>(
            this IEnumerable<T> source, int batchSize)
        {
            using var enumerator = source.GetEnumerator();
            while (enumerator.MoveNext())
                yield return YieldBatchElements(enumerator, batchSize - 1);
        }

        private static IEnumerable<T> YieldBatchElements<T>(
            IEnumerator<T> source, int batchSize)
        {
            yield return source.Current;
            for (int i = 0; i < batchSize && source.MoveNext(); i++)
                yield return source.Current;
        }

        public static void Transform<T>(this IList<T> self, Func<T, T> transformFunc)
        {
            for (int i = 0; i < self.Count; i++)
                self[i] = transformFunc(self[i]);
        }
        
        public static void Resize<T>(this List<T> self, int newSize, Func<T> factory = null)
        {
            var gotBigger = self.Count < newSize;
            self.Capacity = newSize;
            
            if (gotBigger)
            {
                var missingElements = newSize - self.Count;
                for (int i = 0; i < missingElements; i++)
                    self.Add(factory == null ? default : factory());
            }
        }

        public static int IndexOf<T>(this IEnumerable<T> self, T item) where T : IComparable<T>
        {
            int i = -1;
            foreach (var el in self)
            {
                i++;
                if (el != null && el.CompareTo(item) == 0)
                    return i;
            }

            return -1;
        }

        public static IEnumerable<T> AppendRange<T>(this IEnumerable<T> self, IEnumerable<T> other)
        {
            foreach (var item in self)
                yield return item;
            foreach (var item in other)
                yield return item;
        }
    }
}
