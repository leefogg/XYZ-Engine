using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Uniforms
{
    public class Uniform1ui : Uniform
    {
        public Uniform1ui(Shader shader, string uniformName) : base(shader, uniformName)
        {
        }

        public virtual void Set(uint value)
        {
            if (location != -1)
                GL.Uniform1(location, value);
        }
    }
}
