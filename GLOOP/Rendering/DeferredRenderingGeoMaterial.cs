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

            
            shader.DiffuseTexture = DiffuseTexture.BindlessHandle;
            shader.NormalTexture = NormalTexture.BindlessHandle;
            shader.SpecularTexture = SpecularTexture.BindlessHandle;
            shader.IlluminationTexture = IlluminationTexture.BindlessHandle;
            
            /*
            DiffuseTexture.Use(shader.DiffuseTexture = TextureUnit.Texture0);
            NormalTexture.Use(shader.NormalTexture = TextureUnit.Texture1);
            SpecularTexture.Use(shader.SpecularTexture = TextureUnit.Texture2);
            IlluminationTexture.Use(shader.IlluminationTexture = TextureUnit.Texture3);
            */
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
