using OpenTK;
using System;
using System.Collections.Generic;
using System.Text;
using static GLOOP.Extensions.MatrixExtensions;
using static GLOOP.Extensions.Vector3Extensions;
using static GLOOP.Extensions.QuaternionExtensions;
using OpenTK.Mathematics;

namespace GLOOP.Util
{
    public static class MathFunctions
    {
        private static readonly Vector3
            RIGHT = new Vector3(1, 0, 0),
            UP = new Vector3(0, 1, 0),
            IN = new Vector3(0, 0, 1);

        public static Matrix4 CreateProjectionMatrix(int width, int height, float fov, float znear, float zfar)
        {
            var output = new Matrix4();

            var aspectRatio = width / (float)height;
            return CreateProjectionMatrix(aspectRatio, fov, znear, zfar, ref output);
        }

        public static Matrix4 CreateProjectionMatrix(float aspectRatio, float fovDegrees, float znear, float zfar, ref Matrix4 output)
        {
            ToIdentity(ref output);

            var yscale = (float)(1f / Math.Tan(MathHelper.DegreesToRadians(fovDegrees / 2f)) * aspectRatio);
            var xscale = yscale / aspectRatio;
            var frustumlength = zfar - znear;

            output.M11 = xscale;
            output.M22 = yscale;
            output.M33 = -((zfar + znear) / frustumlength);
            output.M34 = -1f;
            output.M43 = -(2f * znear * zfar / frustumlength);
            output.M44 = 0f;

            return output;
        }

        public static Matrix4 CreateViewMatrix(Vector3 position, Vector3 rotationDegrees)
        {
            var output = new Matrix4();
            ToIdentity(ref output);
            Rotate(output, ref output, (float)MathHelper.DegreesToRadians(rotationDegrees.X), RIGHT);
            Rotate(output, ref output, (float)MathHelper.DegreesToRadians(rotationDegrees.Y), UP);
            Rotate(output, ref output, (float)MathHelper.DegreesToRadians(rotationDegrees.Z), IN);
            Negate(ref position);
            Translate(output, ref output, position);
            Negate(ref position);

            return output;
        }

        public static Matrix4 CreateModelMatrix(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var output = new Matrix4();
            ToIdentity(ref output);

            Translate(output, ref output, position);
            Multiply(output, ToRotationMatrix(rotation), ref output);
            Scale(output, ref output, scale);

            return output;
        }

        public static Matrix4 ToRotationMatrix(Quaternion self)
        {
            var matrix = new Matrix4();
            ToIdentity(ref matrix);

            float xy = self.X * self.Y;
            float xz = self.X * self.Z;
            float xw = self.X * self.W;
            float yz = self.Y * self.Z;
            float yw = self.Y * self.W;
            float zw = self.Z * self.W;
            float xSquared = self.X * self.X;
            float ySquared = self.Y * self.Y;
            float zSquared = self.Z * self.Z;
            matrix.M11 = 1 - 2 * (ySquared + zSquared);
            matrix.M12 = 2 * (xy - zw);
            matrix.M13 = 2 * (xz + yw);
            matrix.M14 = 0;
            matrix.M21 = 2 * (xy + zw);
            matrix.M22 = 1 - 2 * (xSquared + zSquared);
            matrix.M23 = 2 * (yz - xw);
            matrix.M24 = 0;
            matrix.M31 = 2 * (xz - yw);
            matrix.M32 = 2 * (yz + xw);
            matrix.M33 = 1 - 2 * (xSquared + ySquared);
            matrix.M34 = 0;
            matrix.M41 = 0;
            matrix.M42 = 0;
            matrix.M43 = 0;
            matrix.M44 = 1;

            return matrix;
        }

        public static double Map(double x, double in_min, double in_max, double out_min, double out_max)
            => (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        public static Vector3 Map(double x, double in_min, double in_max, Vector3 out_min, Vector3 out_max)
        {
            var percent = (float)((x - in_min) / (in_max - in_min));
            return Tween(out_min, out_max, percent);
        }
        public static Quaternion Map(double x, double in_min, double in_max, Quaternion out_min, Quaternion out_max)
        {
            var percent = (float)((x - in_min) / (in_max - in_min));
            return Tween(out_min, out_max, percent);
        }

        public static float Tween(float start, float end, float percent) 
            => start * (1f - percent) + end * percent;
        public static Quaternion Tween(Quaternion start, Quaternion end, float percent)
            => start * (1f - percent) + end * percent;
        public static Vector3 Tween(Vector3 start, Vector3 end, float percent)
            => Tween(start, end, new Vector3(percent));
        public static Vector3 Tween(Vector3 start, Vector3 end, Vector3 percent) 
            => start * (Vector3.One - percent) + (end * percent);
        public static Matrix4 Tween(Matrix4 start, Matrix4 end, float percent)
            => start * (1f - percent) + (end * percent);
    }
}
