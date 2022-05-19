using GLOOP.Rendering;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace TextureArrayTest
{
    public class TextureArrayMaterial : Material
    {
        public TextureArray TextureArray;
        public uint Slice;
        private TextureArrayShader shader;

        public override Shader Shader => shader;

        public TextureArrayMaterial(TextureArrayShader shader)
        {
            this.shader = shader;
        }

        public override void Commit()
        {
            var shader = (TextureArrayShader)Shader;
            shader.Use();
            TextureArray.Use(TextureUnit.Texture0);
            shader.Texture = TextureUnit.Texture0;
            shader.TextureSlice = Slice;
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
