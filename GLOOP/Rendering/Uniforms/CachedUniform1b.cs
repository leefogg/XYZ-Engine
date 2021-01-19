using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Uniforms
{
    public class CachedUniform1b : Uniform1b
    {
        private bool cache;

        public CachedUniform1b(Shader shader, string uniformName) : base(shader, uniformName)
        {
        }

        public override void Set(bool value)
        {
            if (cache != value)
            {
                base.Set(value);
                cache = value;
            }
        }
    }
}
