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
    }
}
