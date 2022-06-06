using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GLOOP.Rendering
{
    [StructLayout(LayoutKind.Explicit, Size = 224)]
    public readonly struct GPUSpotLight
    {
        [FieldOffset(0)] readonly Matrix4 modelMatrix;
        [FieldOffset(64)] readonly Vector3 position;
        [FieldOffset(80)] readonly Vector3 color;
        [FieldOffset(96)] readonly Vector3 direction;
        [FieldOffset(112)] readonly Vector3 scale;
        [FieldOffset(124)] readonly float aspectRatio;
        [FieldOffset(128)] readonly float brightness;
        [FieldOffset(132)] readonly float radius;
        [FieldOffset(136)] readonly float falloffPow;
        [FieldOffset(140)] readonly float angularFalloffPow;
        [FieldOffset(144)] readonly float FOV;
        [FieldOffset(148)] readonly float diffuseScalar;
        [FieldOffset(152)] readonly float specularScalar;
        [FieldOffset(160)] readonly Matrix4 ViewProjection;

        public GPUSpotLight(
            Matrix4 modelMatrix,
            Vector3 position,
            Vector3 color,
            Vector3 direction,
            Vector3 scale,
            float ar,
            float brightness,
            float radius,
            float falloffPow,
            float angularFalloffPow,
            float fov,
            float diffuseScalar,
            float specularScalar,
            Matrix4 ViewProjection
        ) {
            this.modelMatrix = modelMatrix;
            this.position = position;
            this.color = color;
            this.direction = direction;
            this.scale = scale;
            this.aspectRatio = ar;
            this.brightness = brightness;
            this.radius = radius;
            this.falloffPow = falloffPow;
            this.angularFalloffPow = angularFalloffPow;
            this.FOV = fov;
            this.diffuseScalar = diffuseScalar;
            this.specularScalar = specularScalar;
            this.ViewProjection = ViewProjection;
        }
    }
}
