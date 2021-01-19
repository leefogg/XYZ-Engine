using OpenTK.Graphics.ES30;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Uniforms
{
    public class Uniform3f : Uniform
    {
        public Uniform3f(Shader shader, string uniformName) : base(shader, uniformName)
        {
        }

        public virtual void Set(Vector3 value)
        {
            if (location != -1)
                GL.Uniform3(location, value);
        }
    }
}
