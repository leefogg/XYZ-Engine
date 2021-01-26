using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class VirtualVAO
    {
        public DrawElementsIndirectData description;
        public readonly VAO container;
        public Box3 BoundingBox = new Box3();

        public VirtualVAO(DrawElementsIndirectData description, VAO container)
        {
            this.description = description;
            this.container = container;
        }

        public void Draw(PrimitiveType renderMode = PrimitiveType.Triangles)
        {
            container.Bind();

            GL.DrawElementsInstancedBaseVertexBaseInstance(
                renderMode,
                (int)description.NumIndexes,
                DrawElementsType.UnsignedInt,
                (IntPtr)description.FirstIndex,
                (int)description.NumInstances,
                (int)description.BaseVertex,
                (int)description.BaseInstance
            );
        }
    }
}
