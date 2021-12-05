using OpenTK;
using OpenTK.Graphics.ES20;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GLOOP.Rendering
{
    public class StaticPixelShader : Shader
    {
        public StaticPixelShader(string vertPath, string fragPath, IDictionary<string, string> defines = null, string name = null)
            : base(load(vertPath, fragPath, defines), name)
        {
            
        }

        public StaticPixelShader(string vertexShaderSource, string fragmentShaderSource, string name = null)
            : base(load(vertexShaderSource, fragmentShaderSource), name)
        {

        }

        public virtual int AverageSamplesPerFragment => 0;
        public virtual int NumOutputTargets => 1;
    }
}
