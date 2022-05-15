using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
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

        public void Deconstruct(out Vector3 position, out float radius)
        {
            position = Position;
            radius = Radius;
        }
    }
}
