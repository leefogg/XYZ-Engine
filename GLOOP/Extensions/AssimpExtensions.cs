using Assimp;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;
using Quaternion = OpenTK.Mathematics.Quaternion;

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

        public static Node Find(this Node self, Func<Node, bool> critera)
        {
            if (critera(self))
                return self;

            foreach (var child in self.Children)
            {
                Assimp.Node foundChild = child.Find(critera);
                if (foundChild!= null)
                    return foundChild;
            }

            return null;
        }

        public static Matrix4 Copy(this Matrix4 self, Matrix4 other)
        {
            self.Row0 = other.Row0;
            self.Row1 = other.Row1;
            self.Row2 = other.Row2;
            self.Row3 = other.Row3;

            return self;
        }
    }
}
