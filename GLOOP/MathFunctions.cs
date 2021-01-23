using OpenTK;
using System;
using System.Collections.Generic;
using System.Text;
using static GLOOP.MatrixExtensions;
using static GLOOP.Extensions.Vector3Extensions;
using static GLOOP.QuaternionExtensions;
using OpenTK.Mathematics;

namespace GLOOP
{
    public static class MathFunctions
    {
        private static readonly Vector3
            RIGHT   = new Vector3(1, 0, 0),
		    UP      = new Vector3(0, 1, 0),
		    IN      = new Vector3(0, 0, 1);

        public static Matrix4 CreateProjectionMatrix(int width, int height, float fov, float znear, float zfar)
        {
            var output = new Matrix4();
            ToIdentity(ref output);

            var aspectRatio = (float)width / (float)height;
            var yscale = (float)(1f / Math.Tan(ToRadians(fov / 2f)) * aspectRatio);
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

        public static Matrix4 CreateViewMatrix(Vector3 position, Vector3 rotation)
        {
            var output = new Matrix4();
            ToIdentity(ref output);
            Rotate(output, ref output, (float)ToRadians(rotation.X), RIGHT);
            Rotate(output, ref output, (float)ToRadians(rotation.Y), UP);
            Rotate(output, ref output, (float)ToRadians(rotation.Z), IN);
            Negate(ref position);
            Translate(output, ref output, position);
            Negate(ref position);

            return output;
        }

        public static Matrix4 CreateModelMatrix(Vector3 position, Quaternion rotation, Vector3 scale) {
            var output = new Matrix4();
            ToIdentity(ref output);

            Translate(output, ref output, position);
            Multiply(output, ToRotationMatrix(rotation), ref output);
            Scale(output, ref output, scale);

            return output;
        }

        public static double ToRadians(double angdeg) => angdeg / 180.0d * Math.PI;
    }
}
