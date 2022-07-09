using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GLOOP.Rendering
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public readonly struct DrawArraysIndirectData : IDrawIndirectData
    {
        [FieldOffset(00)] public readonly uint Count;
        [FieldOffset(04)] public readonly uint InstanceCount;
        [FieldOffset(08)] public readonly uint First;
        [FieldOffset(12)] public readonly uint BaseInstance;

        public DrawArraysIndirectData(
            uint numVertcies, 
            uint numInstances, 
            uint firstVertexIndex, 
            uint baseInstance)
        {
            Count = numVertcies;
            InstanceCount = numInstances;
            First = firstVertexIndex;
            BaseInstance = baseInstance;
        }

        public void Draw(PrimitiveType renderMode = PrimitiveType.Triangles, int? numInstances = null)
        {
            var instances = numInstances ?? (int)InstanceCount;
            GL.DrawArraysInstancedBaseInstance(
                renderMode,
                (int)First,
                (int)Count,
                instances,
                (int)BaseInstance
            );
            Metrics.ModelsDrawn += instances;
        }

        public override bool Equals(object obj)
        {
            return obj is DrawArraysIndirectData data &&
                   Count == data.Count &&
                   InstanceCount == data.InstanceCount &&
                   First == data.First &&
                   BaseInstance == data.BaseInstance;
        }

        public override int GetHashCode() 
            => HashCode.Combine(Count, InstanceCount, First, BaseInstance);
    }
}
