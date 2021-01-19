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

        public TextureArrayMaterial(TextureArrayShader shader)
            : base(shader)
        {

        }

        public override void SetCameraUniforms(Matrix4 projectionMatrix, Matrix4 viewMatrix, Matrix4 modelMatrix)
        {
            ProjectionMatrix = projectionMatrix;
            ViewMatrix = viewMatrix;
            ModelMatrix = modelMatrix;
        }

        public override void Commit()
        {
            var shader = (TextureArrayShader)this.shader;
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
            return new TextureArrayMaterial((TextureArrayShader)shader)
            {
                TextureArray = TextureArray,
                Slice = Slice
            };
        }
    }
}
