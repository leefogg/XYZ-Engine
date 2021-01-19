using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Uniforms
{
    public class CachedUniform1i : Uniform1i
    {
        private int cache;

        public CachedUniform1i(Shader shader, string uniformName) : base(shader, uniformName)
        {
        }

        public override void Set(int value)
        {
            if (cache != value)
            {
                base.Set(value);
                cache = value;
            }
        }
    }
}
