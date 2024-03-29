﻿using GLOOP.Util.Structures;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Linq;

namespace GLOOP.Rendering.Materials
{
    public class DeferredRenderingGeoMaterial : Material, ISkinnableMaterial
    {
        private const string
            USE_NORMAL = "USE_NORMAL_MAP",
            USE_SPECULAR = "USE_SPECULAR_MAP",
            USE_ILLUM = "USE_ILLUM_MAP",
            USE_SKINNED_MESH = "IS_SKINNED_MESH";
        private static readonly ShaderFactory<DeferredRenderingGeoShader> factory = new ShaderFactory<DeferredRenderingGeoShader>(
            defines => new DeferredRenderingGeoShader(defines), 
            new[] {
                USE_NORMAL,
                USE_SPECULAR,
                USE_ILLUM,
                USE_SKINNED_MESH
            }
        );
        private static readonly Texture2D DefaultNormalTexture = Texture.Gray;
        private static readonly Texture2D DefaultSpecularTexture = Texture.Black;
        private static readonly Texture2D DefaultImmuminationTexture = Texture.Black;

        public Vector2 TextureOffset;
        public Vector2 TextureRepeat = new Vector2(1, 1);
        public Vector3 IlluminationColor = new Vector3(1, 1, 1);
        public Vector3 AlbedoColourTint = new Vector3(1, 1, 1);
        public bool HasWorldpaceUVs;
        private Texture2D[] Textures = new Texture2D[4] { Texture.Error, DefaultNormalTexture, DefaultSpecularTexture, DefaultImmuminationTexture };
        public Texture2D DiffuseTexture
        {
            get => Textures[0];
            set => Textures[0] = value;
        }
        public Texture2D NormalTexture
        {
            get => Textures[1];
            set
            {
                Textures[1] = value;
                LazyShader = null;
            }
        }
        public Texture2D SpecularTexture
        {
            get => Textures[2];
            set {
                Textures[2] = value;
                LazyShader = null;
            }
        }
        public Texture2D IlluminationTexture
        {
            get => Textures[3];
            set
            {
                Textures[3] = value;
                LazyShader = null;
            }
        }

        // Non uniform data
        public bool IsSkinned { get; set; }

        public override Shader Shader => LazyShader ??= GetShaderVarient();

        public Shader LazyShader;

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
            Texture.Use(Textures, TextureUnit.Texture0);
#endif
        }

        public override void SetTextures(Texture2D diffuse, Texture2D normal, Texture2D specular, Texture2D illumination)
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
                TextureRepeat = TextureRepeat,
                IsSkinned = IsSkinned,
            };
        public bool SameTextures(DeferredRenderingGeoMaterial other) => Textures.SequenceEqual(other.Textures);

        private Shader GetShaderVarient()
        {
            return factory.GetVarient(
                NormalTexture != DefaultNormalTexture,
                SpecularTexture != DefaultSpecularTexture,
                IlluminationTexture != DefaultImmuminationTexture,
                IsSkinned
            );
        }
    }
}
