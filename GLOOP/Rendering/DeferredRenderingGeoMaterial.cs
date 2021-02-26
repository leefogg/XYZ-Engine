using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class DeferredRenderingGeoMaterial : Material
    {
        private const string USE_NORMAL = "USE_NORMAL_MAP";
        private const string USE_SPECULAR = "USE_SPECULAR_MAP";
        private const string USE_ILLUM = "USE_ILLUM_MAP";
        private static readonly ShaderFactory<DeferredRenderingGeoShader> factory = new ShaderFactory<DeferredRenderingGeoShader>(
            defines => new DeferredRenderingGeoShader(defines), 
            new[] {
                USE_NORMAL,
                USE_SPECULAR,
                USE_ILLUM
            }
        );

        public Vector2 TextureOffset;
        public Vector2 TextureRepeat = new Vector2(1, 1);
        public Vector3 IlluminationColor = new Vector3(1, 1, 1);
        public Vector3 AlbedoColourTint = new Vector3(1, 1, 1);
        public bool HasWorldpaceUVs;
        private Texture[] Textures = new Texture[4];
        public Texture DiffuseTexture
        {
            get => Textures[0];
            set => Textures[0] = value;
        }
        public Texture NormalTexture
        {
            get => Textures[1];
            set => Textures[1] = value;
        }
        public Texture SpecularTexture
        {
            get => Textures[2];
            set => Textures[2] = value;
        }
        public Texture IlluminationTexture
        {
            get => Textures[3];
            set => Textures[3] = value;
        }

        public override Shader Shader => factory.GetVarient(
            NormalTexture != BaseTexture.Gray, 
            SpecularTexture != BaseTexture.Black, 
            IlluminationTexture != BaseTexture.Black
        );

        public DeferredRenderingGeoMaterial()
        {
        }

        public override void Commit()
        {
            var shader = (DeferredRenderingGeoShader)Shader;
            shader.Use();

#if BINDLESSTEXTURES
            shader.DiffuseTexture = DiffuseTexture.BindlessHandle;
            shader.NormalTexture = NormalTexture.BindlessHandle;
            shader.SpecularTexture = SpecularTexture.BindlessHandle;
            shader.IlluminationTexture = IlluminationTexture.BindlessHandle;
#else
            BaseTexture.Use(Textures, TextureUnit.Texture0);
            shader.DiffuseTexture = TextureUnit.Texture0;
            shader.NormalTexture = TextureUnit.Texture1;
            shader.SpecularTexture = TextureUnit.Texture2;
            shader.IlluminationTexture = TextureUnit.Texture3;
#endif
        }

        public override void SetTextures(Texture diffuse, Texture normal, Texture specular, Texture illumination)
        {
            DiffuseTexture = diffuse;
            NormalTexture = normal;
            SpecularTexture = specular;
            IlluminationTexture = illumination;
        }

        public override Material Clone() 
            => new DeferredRenderingGeoMaterial()
            {
                AlbedoColourTint = AlbedoColourTint,
                DiffuseTexture = DiffuseTexture,
                HasWorldpaceUVs = HasWorldpaceUVs,
                IlluminationColor = IlluminationColor, // TODO: Add connected light's color
                IlluminationTexture = IlluminationTexture,
                ModelMatrix = ModelMatrix,
                NormalTexture = NormalTexture,
                SpecularTexture = SpecularTexture,
                TextureOffset = TextureOffset,
                TextureRepeat = TextureRepeat
            };
    }
}
