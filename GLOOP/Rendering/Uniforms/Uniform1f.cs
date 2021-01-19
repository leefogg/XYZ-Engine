using OpenTK.Graphics.ES30;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Uniforms
{
    public class Uniform1f : Uniform
    {
        public Uniform1f(Shader shader, string uniformName) : base(shader, uniformName)
        {
        }

        public virtual void Set(float value)
        {
            if (location != -1)
                GL.Uniform1(location, value);
        }
    }
}
