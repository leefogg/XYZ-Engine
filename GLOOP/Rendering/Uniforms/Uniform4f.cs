using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Uniforms
{
    public class Uniform4f : Uniform
    {
        public Uniform4f(Shader shader, string uniformName) : base(shader, uniformName)
        {
        }

        public virtual void Set(Vector4 value)
        {
            if (location != -1)
                GL.Uniform4(location, value);
        }
    }
}
