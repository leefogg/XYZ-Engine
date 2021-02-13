using OpenTK;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP {
    public static class MatrixExtensions {
        public static void ToIdentity(ref Matrix4 self) {
            self.M11 = 1;
            self.M12 = 0;
            self.M13 = 0;
            self.M14 = 0;

            self.M21 = 0;
            self.M22 = 1;
            self.M23 = 0;
            self.M24 = 0;

            self.M31 = 0;
            self.M32 = 0;
            self.M33 = 1;
            self.M34 = 0;

            self.M41 = 0;
            self.M42 = 0;
            self.M43 = 0;
            self.M44 = 1;
        }

        public static void Rotate(Matrix4 src, ref Matrix4 dest, float angle, Vector3 axis) {
            if (dest == null)
                dest = new Matrix4();

            float c = (float)Math.Cos(angle);
            float s = (float)Math.Sin(angle);
            float oneminusc = 1.0F - c;
            float xy = axis.X * axis.Y;
            float yz = axis.Y * axis.Z;
            float xz = axis.X * axis.Z;
            float xs = axis.X * s;
            float ys = axis.Y * s;
            float zs = axis.Z * s;
            float f00 = axis.X * axis.X * oneminusc + c;
            float f01 = xy * oneminusc + zs;
            float f02 = xz * oneminusc - ys;
            float f10 = xy * oneminusc - zs;
            float f11 = axis.Y * axis.Y * oneminusc + c;
            float f12 = yz * oneminusc + xs;
            float f20 = xz * oneminusc + ys;
            float f21 = yz * oneminusc - xs;
            float f22 = axis.Z * axis.Z * oneminusc + c;
            float t00 = src.M11 * f00 + src.M21 * f01 + src.M31 * f02;
            float t01 = src.M12 * f00 + src.M22 * f01 + src.M32 * f02;
            float t02 = src.M13 * f00 + src.M23 * f01 + src.M33 * f02;
            float t03 = src.M14 * f00 + src.M24 * f01 + src.M34 * f02;
            float t10 = src.M11 * f10 + src.M21 * f11 + src.M31 * f12;
            float t11 = src.M12 * f10 + src.M22 * f11 + src.M32 * f12;
            float t12 = src.M13 * f10 + src.M23 * f11 + src.M33 * f12;
            float t13 = src.M14 * f10 + src.M24 * f11 + src.M34 * f12;
            dest.M31 = src.M11 * f20 + src.M21 * f21 + src.M31 * f22;
            dest.M32 = src.M12 * f20 + src.M22 * f21 + src.M32 * f22;
            dest.M33 = src.M13 * f20 + src.M23 * f21 + src.M33 * f22;
            dest.M34 = src.M14 * f20 + src.M24 * f21 + src.M34 * f22;
            dest.M11 = t00;
            dest.M12 = t01;
            dest.M13 = t02;
            dest.M14 = t03;
            dest.M21 = t10;
            dest.M22 = t11;
            dest.M23 = t12;
            dest.M24 = t13;
        }

        public static void Translate(Matrix4 src, ref Matrix4 dest, Vector3 vec) {
            if (dest == null)
                dest = new Matrix4();

            dest.M41 += src.M11 * vec.X + src.M21 * vec.Y + src.M31 * vec.Z;
            dest.M42 += src.M12 * vec.X + src.M22 * vec.Y + src.M32 * vec.Z;
            dest.M43 += src.M13 * vec.X + src.M23 * vec.Y + src.M33 * vec.Z;
            dest.M44 += src.M14 * vec.X + src.M24 * vec.Y + src.M34 * vec.Z;
        }

        public static void Scale(Matrix4 src, ref Matrix4 dest, Vector3 scale) {
            if (dest == null)
                dest = new Matrix4();

            dest.M11 = src.M11 * scale.X;
            dest.M12 = src.M12 * scale.X;
            dest.M13 = src.M13 * scale.X;
            dest.M14 = src.M14 * scale.X;

            dest.M21 = src.M21 * scale.Y;
            dest.M22 = src.M22 * scale.Y;
            dest.M23 = src.M23 * scale.Y;
            dest.M24 = src.M24 * scale.Y;

            dest.M31 = src.M31 * scale.Z;
            dest.M32 = src.M32 * scale.Z;
            dest.M33 = src.M33 * scale.Z;
            dest.M34 = src.M34 * scale.Z;
        }

        public static void Multiply(in Matrix4 left, in Matrix4 right, ref Matrix4 dest) {
            if (dest == null)
                dest = new Matrix4();

            float M11 = left.M11 * right.M11 + left.M21 * right.M12 + left.M31 * right.M13 + left.M41 * right.M14;
            float M12 = left.M12 * right.M11 + left.M22 * right.M12 + left.M32 * right.M13 + left.M42 * right.M14;
            float M13 = left.M13 * right.M11 + left.M23 * right.M12 + left.M33 * right.M13 + left.M43 * right.M14;
            float M14 = left.M14 * right.M11 + left.M24 * right.M12 + left.M34 * right.M13 + left.M44 * right.M14;
            float M21 = left.M11 * right.M21 + left.M21 * right.M22 + left.M31 * right.M23 + left.M41 * right.M24;
            float M22 = left.M12 * right.M21 + left.M22 * right.M22 + left.M32 * right.M23 + left.M42 * right.M24;
            float M23 = left.M13 * right.M21 + left.M23 * right.M22 + left.M33 * right.M23 + left.M43 * right.M24;
            float M24 = left.M14 * right.M21 + left.M24 * right.M22 + left.M34 * right.M23 + left.M44 * right.M24;
            float M31 = left.M11 * right.M31 + left.M21 * right.M32 + left.M31 * right.M33 + left.M41 * right.M34;
            float M32 = left.M12 * right.M31 + left.M22 * right.M32 + left.M32 * right.M33 + left.M42 * right.M34;
            float M33 = left.M13 * right.M31 + left.M23 * right.M32 + left.M33 * right.M33 + left.M43 * right.M34;
            float M34 = left.M14 * right.M31 + left.M24 * right.M32 + left.M34 * right.M33 + left.M44 * right.M34;
            float M41 = left.M11 * right.M41 + left.M21 * right.M42 + left.M31 * right.M43 + left.M41 * right.M44;
            float M42 = left.M12 * right.M41 + left.M22 * right.M42 + left.M32 * right.M43 + left.M42 * right.M44;
            float M43 = left.M13 * right.M41 + left.M23 * right.M42 + left.M33 * right.M43 + left.M43 * right.M44;
            float M44 = left.M14 * right.M41 + left.M24 * right.M42 + left.M34 * right.M43 + left.M44 * right.M44;

            dest.M11 = M11;
            dest.M12 = M12;
            dest.M13 = M13;
            dest.M14 = M14;

            dest.M21 = M21;
            dest.M22 = M22;
            dest.M23 = M23;
            dest.M24 = M24;

            dest.M31 = M31;
            dest.M32 = M32;
            dest.M33 = M33;
            dest.M34 = M34;

            dest.M41 = M41;
            dest.M42 = M42;
            dest.M43 = M43;
            dest.M44 = M44;
        }
    }
}
