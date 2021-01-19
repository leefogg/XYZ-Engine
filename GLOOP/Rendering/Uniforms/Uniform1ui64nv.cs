using System;
using System.Collections.Generic;
using System.Text;
using static OpenTK.Graphics.OpenGL4.GL;

namespace GLOOP.Rendering.Uniforms
{
    public class UniformBindlessTexture : Uniform
    {
        public UniformBindlessTexture(Shader shader, string uniformName) : base(shader, uniformName)
        {
        }

        public virtual void Set(ulong bindlessHandle)
        {
            if (location != -1)
                Arb.Uniform1(location, bindlessHandle);
        }
    }
}
