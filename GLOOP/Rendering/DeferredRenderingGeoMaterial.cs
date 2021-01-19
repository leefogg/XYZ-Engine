using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class DeferredRenderingGeoMaterial : Material
    {
        public Vector2 TextureOffset;
        public Vector2 TextureRepeat = new Vector2(1, 1);
        public Vector3 IlluminationColor = new Vector3(1, 1, 1);
        public Vector3 AlbedoColourTint = new Vector3(1, 1, 1);
        public bool HasWorldpaceUVs;
        public Texture DiffuseTexture;
        public Texture NormalTexture;
        public Texture SpecularTexture;
        public Texture IlluminationTexture;

        public DeferredRenderingGeoMaterial(DeferredRenderingGeoShader shader) : base(shader)
        {
        }

        public override void Commit()
        {
            var shader = (DeferredRenderingGeoShader)this.shader;
            shader.Use();
            shader.ModelMatrix = ModelMatrix;

            shader.TextureOffset = TextureOffset;
            shader.TextureRepeat = TextureRepeat;
            shader.HasWorldpaceUVs = HasWorldpaceUVs;
            shader.IlluminationColor = IlluminationColor;
            shader.AlbedoColourTint = AlbedoColourTint;

            //DiffuseTexture.Use(TextureUnit.Texture0);
            //NormalTexture.Use(TextureUnit.Texture1);
            //SpecularTexture.Use(TextureUnit.Texture2);
            //IlluminationTexture.Use(TextureUnit.Texture3);

            shader.DiffuseTexture = DiffuseTexture.BindlessHandle;
            //shader.DiffuseTextureSlice = DiffuseTexture.Slice;
            shader.NormalTexture = NormalTexture.BindlessHandle;
            //shader.NormalTextureSlice = NormalTexture.Slice;
            shader.SpecularTexture = SpecularTexture.BindlessHandle;
            //shader.SpecularTextureSlice = SpecularTexture.Slice;
            shader.IlluminationTexture = IlluminationTexture.BindlessHandle;
           // shader.IlluminationTextureSlice = IlluminationTexture.Slice;
        }

        public override void SetCameraUniforms(Matrix4 projectionMatrix, Matrix4 viewMatrix, Matrix4 modelMatrix)
        {
            ModelMatrix = modelMatrix;
        }

        public override void SetTextures(Texture diffuse, Texture normal, Texture specular, Texture illumination)
        {
            DiffuseTexture = diffuse;
            NormalTexture = normal;
            SpecularTexture = specular;
            IlluminationTexture = illumination;
        }

        public override Material Clone() 
            => new DeferredRenderingGeoMaterial((DeferredRenderingGeoShader)shader);
    }
}
