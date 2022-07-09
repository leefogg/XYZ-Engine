using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GLOOP.Rendering
{
    public interface IDrawIndirectData
    {
        public void Draw(PrimitiveType renderMode = PrimitiveType.Triangles, int? numInstances = null);
    }
}
