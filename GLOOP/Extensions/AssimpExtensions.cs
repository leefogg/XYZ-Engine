using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Extensions
{
    public static class AssimpExtensions
    {
        public static Quaternion ToOpenTK(this Assimp.Quaternion self) => new Quaternion(new Vector3(self.X, self.Y, self.Z), self.W);
        public static Vector3 ToOpenTK(this Assimp.Vector3D self) => new Vector3(self.X, self.Y, self.Z);
        public static Vector2 ToOpenTK(this Assimp.Vector2D self) => new Vector2(self.X, self.Y);
        public static Matrix4 ToOpenTK(this Assimp.Matrix4x4 self)
        {
            var result = new Matrix4(
                new Vector4(self.A1, self.A2, self.A3, self.A4),
                new Vector4(self.B1, self.B2, self.B3, self.B4),
                new Vector4(self.C1, self.C2, self.C3, self.C4),
                new Vector4(self.D1, self.D2, self.D3, self.D4)
            );
            result.Transpose();
            return result;
        }
        public static Matrix3 ToOpenTK(this Assimp.Matrix3x3 self)
        {
            var result = new Matrix3(
                new Vector3(self.A1, self.A2, self.A3),
                new Vector3(self.B1, self.B2, self.B3),
                new Vector3(self.C1, self.C2, self.C3)
            );
            result.Transpose();
            return result;
        }
    }
}
