using GLOOP.Util;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public class StaticTransform : Transform
    {
        public static StaticTransform Default => new StaticTransform(Vector3.Zero, Vector3.One, new Quaternion());

        private Matrix4 _matrix;

        public override Matrix4 Matrix => _matrix;

        public StaticTransform(Matrix4 matrix) => _matrix = matrix;

        public StaticTransform(Vector3 position, Vector3 scale, Quaternion rotation)
            : this(MathFunctions.CreateModelMatrix(position, rotation, scale))
        {
        }
    }
}
