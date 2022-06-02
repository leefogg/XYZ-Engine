using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GLOOP.Rendering
{
    [StructLayout(LayoutKind.Explicit, Size = 128)]
    public readonly struct GPUModel
    {
        [FieldOffset(00)] public readonly Matrix4 Matrix;
        [FieldOffset(64)] public readonly GPUDeferredGeoMaterial Material;

        public GPUModel(Matrix4 matrix, GPUDeferredGeoMaterial material)
        {
            Matrix = matrix;
            Material = material;
        }
    }
}
