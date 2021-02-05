using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public struct Transform
    {
        public static Transform Default => new Transform(Vector3.Zero, Vector3.One, new Quaternion());

        public Vector3 Position;
        public Vector3 Scale;
        public Quaternion Rotation;

        public Transform(Vector3 pos, Vector3 scale, Quaternion rotation)
        {
            Position = pos;
            Scale = scale;
            Rotation = rotation;
        }
    }
}
