﻿using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class SpotLight : Light
    {
        public static new int NumLights { get; private set; } = 0;

        public readonly int SpotLightIndex = NumLights++;

        public readonly Quaternion Rotation;
        public readonly float FOV;
        public readonly float ZNear;
        public readonly float AngularFalloffPower;
        public readonly float AspectRatio;

        public SpotLight(Vector3 position, Quaternion rotation, Vector3 color, float brightness, float falloffPower, LightType type, float angularFalloffPower, float radius, float fov, float znear, float aspectRatio)
            : base(position, color, brightness, radius, falloffPower, type)
        {
            Rotation = rotation;
            FOV = fov;
            ZNear = znear;
            AngularFalloffPower = angularFalloffPower;
            AspectRatio = aspectRatio;
        }
    }
}
