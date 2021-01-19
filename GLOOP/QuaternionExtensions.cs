using OpenTK;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;
using static GLOOP.MatrixExtensions;

namespace GLOOP {
    public static class QuaternionExtensions {
        public static Matrix4 ToRotationMatrix(Quaternion self) {
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

		public static Vector3 GetAngles(this Quaternion rotation)
		{
			var angle = ToEulerAngles(rotation) * 180f / (float)Math.PI;
			return angle;
		}

		public static Vector3 ToEulerAngles(this Quaternion self)
		{
			Vector3 angles;

			// roll (x-axis rotation)
			double sinr_cosp = 2 * (self.W * self.X + self.Y * self.Z);
			double cosr_cosp = 1 - 2 * (self.X * self.X + self.Y * self.Y);
			angles.X = (float)Math.Atan2(sinr_cosp, cosr_cosp);

			// pitch (y-axis rotation)
			double sinp = 2 * (self.W * self.Y - self.Z * self.X);
			if (Math.Abs(sinp) >= 1)
				angles.Y = (float)Math.CopySign(Math.PI / 2, sinp); // use 90 degrees if out of range
			else
				angles.Y = (float)Math.Asin(sinp);

			// yaw (z-axis rotation)
			double siny_cosp = 2 * (self.W * self.Z + self.X * self.Y);
			double cosy_cosp = 1 - 2 * (self.Y * self.Y + self.Z * self.Z);
			angles.Z = (float)Math.Atan2(siny_cosp, cosy_cosp);

			return angles;
		}
	}
}
