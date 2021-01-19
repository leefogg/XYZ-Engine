using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Uniforms
{
    public class CachedUniform16f : Uniform16f
    {
        private Matrix4 cache;

        public CachedUniform16f(Shader shader, string uniformName) : base(shader, uniformName)
        {
        }

        public override void Set(Matrix4 mat)
        {
            if (cache != mat)
            {
                base.Set(mat);
                cache = mat;
            }
        }
    }
}
