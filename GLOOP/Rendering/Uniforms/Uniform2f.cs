using OpenTK.Graphics.ES30;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Uniforms
{
    public class Uniform2f : Uniform
    {
        public Uniform2f(Shader shader, string uniformName) : base(shader, uniformName)
        {
        }

        public virtual void Set(Vector2 value)
        {
            if (location != -1)
                GL.Uniform2(location, value);
        }
    }
}
