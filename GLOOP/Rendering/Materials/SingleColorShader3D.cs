using GLOOP.Rendering.Uniforms;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Materials
{
    public class SingleColorShader3D : StaticPixelShader
    {
        public readonly Uniform16f modelMatrix;
        private Uniform4f color;

        public Matrix4 ModelMatrix
        {
            set => modelMatrix.Set(value);
        }
        public Vector4 Color
        {
            set => color.Set(value);
        }

        public SingleColorShader3D(IDictionary<string, string> defines = null, string name = null) 
            : base("assets/shaders/SingleColor/3D/vertexShader.vert",
                   "assets/shaders/SingleColor/3D/fragmentShader.frag", defines, name)
        {
            modelMatrix = new Uniform16f(this, "ModelMatrix");

            color = new Uniform4f(this, "color");
        }
    }
}
