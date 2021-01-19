using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GLOOP.Rendering
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = sizeof(uint) * 5)]
    public struct DrawElementsIndirectData
    {
        public uint NumIndexes;
        public uint NumInstances;
        public uint FirstIndex;
        public uint BaseVertex;
        public uint BaseInstance;

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
