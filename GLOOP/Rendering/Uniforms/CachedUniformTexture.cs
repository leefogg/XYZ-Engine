using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Uniforms
{
    public class CachedUniformTexture : UniformTexture
    {
        private TextureUnit cache;

        public CachedUniformTexture(Shader shader, string uniformName) : base(shader, uniformName)
        {
        }

        public override void Set(TextureUnit unit)
        {
            if (cache != unit)
            {
                base.Set(unit);
                cache = unit;
            }
        }
    }
}
