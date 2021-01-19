using OpenTK;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Extensions
{
    public static class StringExtensions
    {
        public static Vector4 ParseVector4(this string tuple)
        {
            var parts = tuple.Split(' ');
            var x = float.Parse(parts[0]);
            var y = float.Parse(parts[1]);
            var z = float.Parse(parts[2]);
            var w = float.Parse(parts[3]);
            return new Vector4(x, y, z, w);
        }
        public static Vector3 ParseVector3(this string tuple)
        {
            var parts = tuple.Split(' ');
            var x = float.Parse(parts[0]);
            var y = float.Parse(parts[1]);
            var z = float.Parse(parts[2]);
            return new Vector3(x, y, z);
        }

        public static Vector2 ParseVector2(this string tuple)
        {
            var parts = tuple.Split(' ');
            var x = float.Parse(parts[0]);
            var y = float.Parse(parts[1]);
            return new Vector2(x, y);
        }

        public static Quaternion ParseQuaternion(this string tuple)
        {
            var parts = tuple.Split(' ');
            var x = float.Parse(parts[0]);
            var y = float.Parse(parts[1]);
            var z = float.Parse(parts[2]);
            var w = float.Parse(parts[3]);
            return new Quaternion(x, y, z, w);
        }
    }
}
