using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class VirtualVAO
    {
        public readonly DrawElementsIndirectData Description;
        public readonly VAO Container;
        public Box3 BoundingBox;

        public VirtualVAO(DrawElementsIndirectData description, VAO container)
        {
            Description = description;
            Container = container;
        }

        public void Draw(PrimitiveType renderMode = PrimitiveType.Triangles)
        {
            Container.Bind();

            GL.DrawElementsInstancedBaseVertexBaseInstance(
                renderMode,
                (int)Description.NumIndexes,
                DrawElementsType.UnsignedShort,
                (IntPtr)Description.FirstIndex,
                (int)Description.NumInstances,
                (int)Description.BaseVertex,
                (int)Description.BaseInstance
            );
            Metrics.ModelsDrawn++;
        }
    }
}
