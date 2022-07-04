using GLOOP.Util;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public class DynamicTransform : Transform
    {
        public static DynamicTransform Default => new DynamicTransform(Vector3.Zero, Vector3.One, new Quaternion());

        public Vector3 Position;
        public Vector3 Scale;
        public Quaternion Rotation;

        public DynamicTransform(Matrix4 matrix)
        {
            Position = matrix.ExtractTranslation();
            Rotation = matrix.ExtractRotation();
            Scale = matrix.ExtractScale();
        }
        public DynamicTransform(Vector3 position, Vector3 scale, Quaternion rotation) 
        {
            Position = position;
            Scale = scale;
            Rotation = rotation;
        }

        public override Matrix4 Matrix => MathFunctions.CreateModelMatrix(Position, Rotation, Scale);
    }
}
