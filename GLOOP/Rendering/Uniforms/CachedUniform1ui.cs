using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Uniforms
{
    public class CachedUniform1ui : Uniform1ui
    {
        private uint cache;

        public CachedUniform1ui(Shader shader, string uniformName) : base(shader, uniformName)
        {
        }

        public override void Set(uint value)
        {
            if (cache != value)
            {
                base.Set(value);
                cache = value;
            }
        }
    }
}
