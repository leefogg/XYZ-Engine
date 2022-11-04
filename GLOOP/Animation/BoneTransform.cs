using GLOOP.Extensions;
using GLOOP.Util;
using OpenTK.Mathematics;

namespace GLOOP.Animation
{
    public struct BoneTransform : Transform
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

        public Matrix4 Matrix => MathFunctions.CreateModelMatrix(Position, Rotation, Vector3.One);

        public BoneTransform Clone() => new BoneTransform(Position, Rotation);
    }
}
