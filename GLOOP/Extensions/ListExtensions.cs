using OpenTK;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

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
    }
}
