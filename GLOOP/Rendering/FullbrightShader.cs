using GLOOP.Rendering.Uniforms;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class FullbrightShader : StaticPixelShader
    {
        public readonly Uniform16f projectionMatrix, viewMatrix, modelMatrix;
        public readonly UniformBindlessTexture diffuse;

        public ulong DiffuseTexture
        {
            set => diffuse.Set(value);
        }
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

        public FullbrightShader() 
            : base("assets/shaders/FullBright/3D/VertexShader.vert", "assets/shaders/FullBright/3D/FragmentShader.frag", name: "Fullbright")
        {
            diffuse = new UniformBindlessTexture(this, "texture0");

            projectionMatrix = new Uniform16f(this, "ProjectionMatrix");
            viewMatrix = new Uniform16f(this, "ViewMatrix");
            modelMatrix = new Uniform16f(this, "ModelMatrix");
        }
    }
}
