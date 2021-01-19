using OpenTK.Graphics.ES30;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Uniforms
{
    public class Uniform16f : Uniform
    {
        public Uniform16f(Shader shader, string uniformName) : base(shader, uniformName)
        {
        }

        public virtual void Set(Matrix4 mat)
        {
            if (location != -1)
                GL.UniformMatrix4(location, false, ref mat);
        }
    }
}
