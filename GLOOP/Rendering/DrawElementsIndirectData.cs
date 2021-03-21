using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GLOOP.Rendering
{
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public readonly struct DrawElementsIndirectData
    {
        [FieldOffset(00)] public readonly uint NumIndexes;
        [FieldOffset(04)] public readonly uint NumInstances;
        [FieldOffset(08)] public readonly uint FirstIndex;
        [FieldOffset(12)] public readonly uint BaseVertex;
        [FieldOffset(16)] public readonly uint BaseInstance;

        public DrawElementsIndirectData(
            uint numIndexes,
            uint firstIndex,
            uint baseVertex,
            uint numInstances,
            uint baseInstance)
        {
            NumIndexes = numIndexes;
            FirstIndex = firstIndex;
            BaseVertex = baseVertex;
            NumInstances = numInstances;
            BaseInstance = baseInstance;
        }

        public override bool Equals(object obj)
        {
            if (obj is DrawElementsIndirectData data)
            {
                return data.BaseInstance == BaseVertex
                    && data.BaseInstance == BaseInstance
                    && data.FirstIndex == FirstIndex
                    && data.NumIndexes == NumIndexes
                    && data.NumInstances == NumInstances;
            }
            else
            {
                return false;
            }

        }
    }
}
