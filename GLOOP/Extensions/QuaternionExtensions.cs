using OpenTK.Mathematics;
using System;
namespace GLOOP.Extensions
{
    public static class QuaternionExtensions
    {
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
