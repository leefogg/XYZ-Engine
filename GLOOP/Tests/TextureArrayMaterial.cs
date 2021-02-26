using GLOOP.Rendering;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Tests
{
    public class TextureArrayMaterial : Material
    {
        public Matrix4 ProjectionMatrix, ViewMatrix;
        public Rendering.TextureArray TextureArray;
        public uint Slice;
        private TextureArrayShader shader;

        public override Shader Shader => shader;

        public TextureArrayMaterial(TextureArrayShader shader)
        {
            this.shader = shader;
        }

        public override void SetCameraUniforms(Matrix4 projectionMatrix, Matrix4 viewMatrix, Matrix4 modelMatrix)
        {
            ProjectionMatrix = projectionMatrix;
            ViewMatrix = viewMatrix;
            ModelMatrix = modelMatrix;
        }

        public override void Commit()
        {
            var shader = (TextureArrayShader)this.Shader;
            shader.Use();
            TextureArray.Use(TextureUnit.Texture0);
            shader.Texture = TextureUnit.Texture0;
            shader.TextureSlice = Slice;

            shader.ProjectionMatrix = ProjectionMatrix;
            shader.ViewMatrix = ViewMatrix;
            shader.ModelMatrix = ModelMatrix;
        }

        public override Material Clone()
        {
            return new TextureArrayMaterial((TextureArrayShader)Shader)
            {
                TextureArray = TextureArray,
                Slice = Slice
            };
        }
    }
}
