using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GLOOP.Rendering
{
    [StructLayout(LayoutKind.Explicit, Size = 48)]
    public readonly struct GPUPointLight
    {
        [FieldOffset(00)] readonly Vector3 position;
        [FieldOffset(12)] readonly float brightness;
        [FieldOffset(16)] readonly Vector3 color;
        [FieldOffset(28)] readonly float radius;
        [FieldOffset(32)] readonly float falloffPow;
        [FieldOffset(36)] readonly float diffuseScalar;
        [FieldOffset(40)] readonly float specularScalar;

        public GPUPointLight(
            Vector3 position,
            Vector3 color,
            float brightness,
            float radius,
            float falloffPow,
            float diffuseScalar,
            float specularScalar
        ) {
            this.position = position;
            this.color = color;
            this.brightness = brightness;
            this.radius = radius;
            this.falloffPow = falloffPow;
            this.diffuseScalar = diffuseScalar;
            this.specularScalar = specularScalar;
        }
    };
}
