﻿using GLOOP.Util;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public class StaticTransform
    {
        public static StaticTransform Default => new StaticTransform(Vector3.Zero, Vector3.One, new Quaternion());

        public Matrix4 Matrix { get; private set; }

        public StaticTransform(Vector3 position, Vector3 scale, Quaternion rotation)
        {
            Matrix = MathFunctions.CreateModelMatrix(position, rotation, scale);
        }
    }
}
