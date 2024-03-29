﻿using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GLOOP.Rendering
{
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public readonly struct DrawElementsIndirectData : IDrawIndirectData
    {
        [FieldOffset(00)] public readonly uint Count;
        [FieldOffset(04)] public readonly uint InstanceCount;
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
            Count = numIndexes;
            FirstIndex = firstIndex;
            BaseVertex = baseVertex;
            InstanceCount = numInstances;
            BaseInstance = baseInstance;
        }

        public void Draw(PrimitiveType renderMode = PrimitiveType.Triangles, int? numInstances = null)
        {
            var instances = numInstances ?? (int)InstanceCount;
            GL.DrawElementsInstancedBaseVertexBaseInstance(
                renderMode,
                (int)Count,
                DrawElementsType.UnsignedShort,
                (IntPtr)FirstIndex,
                instances,
                (int)BaseVertex,
                (int)BaseInstance
            );
            Metrics.ModelsDrawn += instances;
        }

        public override bool Equals(object obj)
        {
            return obj is DrawElementsIndirectData data &&
                   Count == data.Count &&
                   InstanceCount == data.InstanceCount &&
                   FirstIndex == data.FirstIndex &&
                   BaseVertex == data.BaseVertex &&
                   BaseInstance == data.BaseInstance;
        }

        public override int GetHashCode() 
            => HashCode.Combine(Count, InstanceCount, FirstIndex, BaseVertex, BaseInstance);
    }
}
