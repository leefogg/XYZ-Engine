using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public abstract class Light
    {
        public static int NumLights { get; private set; } = 0;

        public readonly int LightIndex = NumLights++;

        public Vector3 Position { get; set; }
        public Vector3 Color { get; }
        public float Brightness { get; }
        public string DiffuseColor { get; }
        public float Radius { get; }
        public float FalloffPower { get; }
        public LightType Type { get; set; }

        public Light(Vector3 position, Vector3 color, float brightness, float radius, float falloffPow, LightType type)
        {
            Position = position;
            Color = color;
            Brightness = brightness;
            Radius = radius;
            FalloffPower = falloffPow;
            Type = type;
        }

        public void GetLightingScalars(out float diffuseScalar, out float specularScalar)
        {
            //TODO: Probably could make LightType a bitfield and use bits to get each variable
            diffuseScalar = 1;
            specularScalar = 1;
            switch (Type)
            {
                case LightType.Diffuse:
                    specularScalar = 0;
                    break;
                case LightType.Specular:
                    diffuseScalar = 0;
                    break;
            }
        }
    }
}
