using OpenTK.Graphics.ES30;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Uniforms
{
    public class Uniform1b : Uniform
    {
        public Uniform1b(Shader shader, string uniformName) : base(shader, uniformName)
        {
        }

        public virtual void Set(bool value)
        {
            if (location != -1)
                GL.Uniform1(location, value ? 1 : 0);
        }
    }
}
