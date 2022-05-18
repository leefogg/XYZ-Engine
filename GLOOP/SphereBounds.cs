using GLOOP.Rendering;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace GLOOP
{
    public readonly struct SphereBounds
    {
        public readonly Vector3 Position;
        public readonly float Radius;

        public SphereBounds(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }

        public SphereBounds(Box3 boundingBox, Transform objectTransform)
        {
            Position = boundingBox.Center + objectTransform.Position;
            var size = boundingBox.Size * objectTransform.Scale;
            Radius = Math.Max(Math.Max(size.X, size.Y), size.Z) / 2f;
        }

        public void Deconstruct(out Vector3 position, out float radius)
        {
            position = Position;
            radius = Radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IntersectsPlane(Vector4 planeEquation, Vector3 Position, float Radius)
        {
            return planeEquation.X * Position.X + planeEquation.Y * Position.Y + planeEquation.Z * Position.Z + planeEquation.W <= -Radius;
        }
    }
}
