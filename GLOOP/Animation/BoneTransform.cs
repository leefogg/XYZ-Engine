using GLOOP.Util;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Animation
{
    public class BoneTransform : Transform
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public BoneTransform(Matrix4 matrix)
        {
            Position = matrix.ExtractTranslation();
            Rotation = matrix.ExtractRotation();
        }
        public BoneTransform(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public override Matrix4 Matrix => MathFunctions.CreateModelMatrix(Position, Rotation, Vector3.One);
    }
}
