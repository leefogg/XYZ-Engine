using GLOOP.Rendering;
using GLOOP.Rendering.Uniforms;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Tests
{
    public class TextureArrayShader : StaticPixelShader
    {

        private Uniform16f projectionMatrix, viewMatrix, modelMatrix;
        private UniformTexture textureArray;
        private Uniform1ui slice;

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
        public TextureUnit Texture
        {
            set => textureArray.Set(value);
        }
        public uint TextureSlice
        {
            set => slice.Set(value);
        }

        public TextureArrayShader(IDictionary<string, string> defines = null) 
            : base("assets/shaders/texturearraytest/vertexshader.vert", "assets/shaders/texturearraytest/fragmentshader.frag", defines)
        {
            projectionMatrix = new Uniform16f(this, "ProjectionMatrix");
            viewMatrix = new Uniform16f(this, "ViewMatrix");
            modelMatrix = new Uniform16f(this, "ModelMatrix");

            textureArray = new UniformTexture(this, "texture0");
            slice = new Uniform1ui(this, "slice");
        }
    }
}
