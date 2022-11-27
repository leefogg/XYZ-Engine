using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GLOOP.Rendering
{
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public readonly struct GPUDeferredGeoMaterial
    {
        [FieldOffset(00)] public readonly Vector3 IlluminationColor;
        [FieldOffset(16)] public readonly Vector3 AlbedoColourTint;
        [FieldOffset(32)] public readonly Vector2 TextureRepeat;
        [FieldOffset(40)] public readonly Vector2 TextureOffset;
        [FieldOffset(48)] public readonly float NormalStrength;
        [FieldOffset(52)] public readonly bool IsWorldSpaceUVs;
        [FieldOffset(56)] public readonly uint BoneStartIdx;

        public GPUDeferredGeoMaterial(Vector3 illuminationColor, Vector3 albedoColourTint, Vector2 textureRepeat, Vector2 textureOffset, float normalStrength, bool isWorldSpaceUVs, uint boneStartIndx)
        {
            IlluminationColor = illuminationColor;
            AlbedoColourTint = albedoColourTint;
            TextureRepeat = textureRepeat;
            TextureOffset = textureOffset;
            NormalStrength = normalStrength;
            IsWorldSpaceUVs = isWorldSpaceUVs;
            BoneStartIdx = boneStartIndx;
        }
    }
}
