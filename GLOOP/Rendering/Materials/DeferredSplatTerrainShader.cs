using GLOOP.Rendering.Uniforms;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Materials
{
    public class DeferredSplatTerrainShader : StaticPixelShader
    {
        private CachedUniformTexture diffuseTex, splatTex, blendLayer0Tex0, blendLayer0Tex1, blendLayer0Tex2, blendLayer0Tex3;
        private Uniform16f modelMatrix;

        private CachedUniform1f baseTextureTileAmount;
        private CachedUniform4f blendLayer0TileAmount;
        private CachedUniform1f specularPower;

        public Matrix4 ModelMatrix
        {
            set => modelMatrix.Set(value);
        }

        public float BaseTextureTileAmount
        {
            set => baseTextureTileAmount.Set(value);
        }
        public Vector4 BlendLayer0TileAmounts
        {
            set => blendLayer0TileAmount.Set(value);
        }
        public float SpecularPower
        {
            set => specularPower.Set(value);
        }

        public DeferredSplatTerrainShader() 
            : base("assets/shaders/Deferred/GeoPass/Terrain.vert", "assets/shaders/Deferred/GeoPass/Terrain.frag", null, "Deferred terrain geometry pass")
        {
            diffuseTex = new CachedUniformTexture(this, "diffuseTex");
            splatTex = new CachedUniformTexture(this, "splatTex");
            blendLayer0Tex0 = new CachedUniformTexture(this, "blendLayer0Tex0");
            blendLayer0Tex1 = new CachedUniformTexture(this, "blendLayer0Tex1");
            blendLayer0Tex2 = new CachedUniformTexture(this, "blendLayer0Tex2");
            blendLayer0Tex3 = new CachedUniformTexture(this, "blendLayer0Tex3");

            diffuseTex.Set(OpenTK.Graphics.OpenGL4.TextureUnit.Texture0);
            splatTex.Set(OpenTK.Graphics.OpenGL4.TextureUnit.Texture1);
            blendLayer0Tex0.Set(OpenTK.Graphics.OpenGL4.TextureUnit.Texture2);
            blendLayer0Tex1.Set(OpenTK.Graphics.OpenGL4.TextureUnit.Texture3);
            blendLayer0Tex2.Set(OpenTK.Graphics.OpenGL4.TextureUnit.Texture4);
            blendLayer0Tex3.Set(OpenTK.Graphics.OpenGL4.TextureUnit.Texture5);

            modelMatrix = new Uniform16f(this, "ModelMatrix");

            baseTextureTileAmount = new CachedUniform1f(this, "BaseTextureTileAmount");
            blendLayer0TileAmount = new CachedUniform4f(this, "BlendLayer0TileAmount");
            specularPower = new CachedUniform1f(this, "SpecularPower");
        }
    }
}
