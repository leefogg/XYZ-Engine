using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GLOOP.Rendering
{
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public struct DrawElementsIndirectData
    {
        [FieldOffset(00)] public uint NumIndexes;
        [FieldOffset(04)] public uint NumInstances;
        [FieldOffset(08)] public uint FirstIndex;
        [FieldOffset(12)] public uint BaseVertex;
        [FieldOffset(16)] public uint BaseInstance;

        public DrawElementsIndirectData(
            uint numIndexes,
            uint instanceCount,
            uint firstIndex,
            uint baseVertex,
            uint baseInstance)
        {
            NumIndexes = numIndexes;
            NumInstances = instanceCount;
            FirstIndex = firstIndex;
            BaseVertex = baseVertex;
            BaseInstance = baseInstance;
        }
    }
}
