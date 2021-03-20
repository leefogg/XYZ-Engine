using GLOOP.Rendering;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace GLOOP
{
    public struct Sphere
    {
        public Vector3 Position;
        public float Radius;

        public Sphere(Box3 boundingBox, Transform objectTransform)
        {
            Position = boundingBox.Center + objectTransform.Position;
            var size = boundingBox.Size * objectTransform.Scale;
            Radius = Math.Max(Math.Max(size.X, size.Y), size.Z) / 2f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IntersectsPlane(Vector4 planeEquation, Vector3 Position, float Radius)
        {
            return planeEquation.X * Position.X + planeEquation.Y * Position.Y + planeEquation.Z * Position.Z + planeEquation.W <= -Radius;
        }
    }
}
