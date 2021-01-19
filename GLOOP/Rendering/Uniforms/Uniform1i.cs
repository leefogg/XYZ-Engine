using OpenTK.Graphics.ES30;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Uniforms
{
    public class Uniform1i : Uniform
    {
        public Uniform1i(Shader shader, string uniformName) : base(shader, uniformName)
        {
        }

        public virtual void Set(int value)
        {
            if (location != -1)
                GL.Uniform1(location, value);
        }
    }
}
