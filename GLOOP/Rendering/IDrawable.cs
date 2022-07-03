using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    internal interface IDrawable
    {
        public void Draw(PrimitiveType renderMode = PrimitiveType.Triangles, int numInstances = 1);
    }
}
