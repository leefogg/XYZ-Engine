using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Uniforms
{
    public class CachedUniform1f : Uniform1f
    {
        private float cache;

        public CachedUniform1f(Shader shader, string uniformName) : base(shader, uniformName)
        {
        }

        public override void Set(float value)
        {
            if (cache != value)
            {
                base.Set(value);
                cache = value;
            }
        }
    }
}
