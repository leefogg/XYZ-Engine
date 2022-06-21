using OpenTK;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class PointLight : Light
    {
        public static new int NumLights { get; private set; } = 0;

        public readonly int PointLightIndex = NumLights++;

        public PointLight(Vector3 position, Vector3 color, float brightness, float radius, float falloffPow, LightType type)
            : base(position, color, brightness, radius, falloffPow, type)
        {
        }
    }
}
