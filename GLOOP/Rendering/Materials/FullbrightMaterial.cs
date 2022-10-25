using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Materials
{
    public class FullbrightMaterial : Material
    {
        public Texture2D diffuse;
        private FullbrightShader shader;

        public override Shader Shader => shader;

        public FullbrightMaterial(FullbrightShader shader)
        {
            this.shader = shader;
        }

        public override void SetTextures(Texture2D diffuse, Texture2D normal, Texture2D specular, Texture2D illumination)
        {
            this.diffuse = diffuse;
        }

        public override void Commit()
        {
            shader.Use();
            shader.ModelMatrix = ModelMatrix;
            diffuse.Use();
            shader.DiffuseTexture = TextureUnit.Texture0;
        }

        public override Material Clone()
        {
            return new FullbrightMaterial(shader)
            {
                diffuse = diffuse
            };
        }
    }
}
