using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class DeferredSplatTerrainMaterial : Material
    {
        private DeferredSplatTerrainShader shader;

        public Texture2D[] Textures = new Texture2D[6];
        public Texture2D BaseTexture
        {
            get => Textures[0];
            set => Textures[0] = value;
        }
        public Texture2D SplatTexture
        {
            get => Textures[1];
            set => Textures[1] = value;
        }
        public Texture2D BlendLayer0Texture0
        {
            get => Textures[2];
            set => Textures[2] = value;
        }
        public Texture2D BlendLayer0Texture1
        {
            get => Textures[3];
            set => Textures[3] = value;
        }
        public Texture2D BlendLayer0Texture2
        {
            get => Textures[4];
            set => Textures[4] = value;
        }
        public Texture2D BlendLayer0Texture3
        {
            get => Textures[5];
            set => Textures[5] = value;
        }

        public float BaseTextureTileAmount = 1;
        public Vector4 TileAmounts = new Vector4();
        public float SpecularPower = 0.1f;

        public override Shader Shader => shader;

        public DeferredSplatTerrainMaterial(DeferredSplatTerrainShader shader)
        {
            this.shader = shader;
        }

        public override void Commit()
        {
            shader.Use();

            shader.ModelMatrix = ModelMatrix;
            shader.BaseTextureTileAmount = BaseTextureTileAmount;
            shader.BlendLayer0TileAmounts = TileAmounts;
            shader.SpecularPower = SpecularPower;

            Texture.Use(Textures, OpenTK.Graphics.OpenGL4.TextureUnit.Texture0);
        }

        public override Material Clone()
        {
            throw new NotImplementedException();
        }
    }
}
