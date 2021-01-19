using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Uniforms
{
    public class CachedUniform3f : Uniform3f
    {
        private Vector3 cache;

        public CachedUniform3f(Shader shader, string uniformName) : base(shader, uniformName)
        {
        }

        public override void Set(Vector3 value)
        {
            if (cache != value)
            {
                base.Set(value);
                cache = value;
            }
        }
    }
}
