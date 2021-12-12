using GLOOP.Rendering.Uniforms;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Materials
{
    public class SingleColorShader2D : StaticPixelShader
    {
        private Uniform4f color;
        public Vector4 Color
        {
            set => color.Set(value);
        }

        public SingleColorShader2D(IDictionary<string, string> defines = null, string name = null) 
            : base("assets/shaders/SingleColor/2D/vertexShader.vert",
                   "assets/shaders/SingleColor/2D/fragmentShader.frag", defines, name)
        {
            color = new Uniform4f(this, "color");
            color.Set(new Vector4(1,1,1,0));
        }
    }
}
