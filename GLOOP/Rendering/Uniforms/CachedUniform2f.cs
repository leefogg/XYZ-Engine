using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Uniforms
{
    public class CachedUniform2f : Uniform2f
    {
        private Vector2 cache;

        public CachedUniform2f(Shader shader, string uniformName) : base(shader, uniformName)
        {
        }

        public override void Set(Vector2 value)
        {
            if (cache != value)
            {
                base.Set(value);
                cache = value;
            }
        }
    }
}
