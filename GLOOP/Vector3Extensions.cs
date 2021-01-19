using OpenTK;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public static class Vector3Extensions
    {
        public static void Negate(ref Vector3 self)
        {
            self.X = -self.X;
            self.Y = -self.Y;
            self.Z = -self.Z;
        }
        public static Vector3 Negated(this Vector3 self)
        {
            self.X *= -1;
            self.Y *= -1;
            self.Z *= -1;
            return self;
        }

        public static void Multiply(ref Vector3 src, float scaler)
        {
            src.X *= scaler;
            src.Y *= scaler;
            src.Z *= scaler;
        }

        public static void Multiply(ref Vector3 src, Vector3 scaler)
        {
            src.X *= scaler.X;
            src.Y *= scaler.Y;
            src.Z *= scaler.Z;
        }

        public static void Multiply(ref Vector2 src, float scaler)
        {
            src.X *= scaler;
            src.Y *= scaler;
        }

        public static void Multiply(ref Vector2 src, Vector3 scaler)
        {
            src.X *= scaler.X;
            src.Y *= scaler.Y;
        }
    }
}
