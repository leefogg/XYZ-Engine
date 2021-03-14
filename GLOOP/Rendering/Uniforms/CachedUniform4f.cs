using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Uniforms
{
    public class CachedUniform4f : Uniform4f
    {
        private Vector4 cache;

        public CachedUniform4f(Shader shader, string uniformName) : base(shader, uniformName)
        {
        }

        public override void Set(Vector4 value)
        {
            if (cache != value)
            {
                base.Set(value);
                cache = value;
            }
        }
    }
}
