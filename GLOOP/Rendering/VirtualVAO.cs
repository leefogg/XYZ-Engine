using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class VirtualVAO : IDrawable
    {
        public readonly IDrawIndirectData Description;
        public readonly VAO Container;
        public Box3 BoundingBox;

        public VirtualVAO(IDrawIndirectData description, VAO container)
        {
            Description = description;
            Container = container;
        }

        public void Draw(PrimitiveType renderMode = PrimitiveType.Triangles, int numInstances = 1)
        {
            Container.Bind();
            Description.Draw(renderMode, numInstances);
        }
    }
}
