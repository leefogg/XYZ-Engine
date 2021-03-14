using GLOOP.Rendering.Uniforms;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Materials
{
    public class SingleColorShader : StaticPixelShader
    {
        public readonly Uniform16f projectionMatrix, viewMatrix, modelMatrix;
        private Uniform4f color;

        public Matrix4 ProjectionMatrix
        {
            set => projectionMatrix.Set(value);
        }
        public Matrix4 ViewMatrix
        {
            set => viewMatrix.Set(value);
        }
        public Matrix4 ModelMatrix
        {
            set => modelMatrix.Set(value);
        }
        public Vector4 Color
        {
            set => color.Set(value);
        }

        public SingleColorShader(IDictionary<string, string> defines = null, string name = null) 
            : base("assets/shaders/SingleColor/basic.vert",
                   "assets/shaders/SingleColor/basic.frag", defines, name)
        {
            projectionMatrix = new Uniform16f(this, "ProjectionMatrix");
            viewMatrix = new Uniform16f(this, "ViewMatrix");
            modelMatrix = new Uniform16f(this, "ModelMatrix");

            color = new Uniform4f(this, "color");
        }
    }
}
