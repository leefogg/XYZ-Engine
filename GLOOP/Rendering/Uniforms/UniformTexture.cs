using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Uniforms
{
    public class UniformTexture : Uniform
    {
        public UniformTexture(Shader shader, string uniformName) : base(shader, uniformName)
        {
        }

        public virtual void Set(TextureUnit unit)
        {
            if (location != -1)
                GL.Uniform1(location, unit - TextureUnit.Texture0);
        }
    }
}
