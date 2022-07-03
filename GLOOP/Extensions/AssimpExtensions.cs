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
            var result = new Matrix4();
            result.Row0 = new Vector4(self.A1, self.A2, self.A3, self.A4);
            result.Row1 = new Vector4(self.D1, self.B2, self.B3, self.B4);
            result.Row2 = new Vector4(self.C1, self.C2, self.C3, self.C4);
            result.Row3 = new Vector4(self.D1, self.D2, self.D3, self.D4);
            return result;
        }
        public static Matrix3 ToOpenTK(this Assimp.Matrix3x3 self)
        {
            var result = new Matrix3();

            for (int row = 0; row < 3; row++)
                for (int column = 0; column < 3; column++)
                    result[row, column] = self[row, column];
            //result.Transpose();
            return result;
        }
    }
}
