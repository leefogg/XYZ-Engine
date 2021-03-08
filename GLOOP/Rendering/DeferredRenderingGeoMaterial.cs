﻿using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class DeferredRenderingGeoMaterial : Material
    {
        private const string 
            USE_NORMAL = "USE_NORMAL_MAP",
            USE_SPECULAR = "USE_SPECULAR_MAP",
            USE_ILLUM = "USE_ILLUM_MAP";
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
        private Texture2D[] Textures = new Texture2D[4] { Texture.Error, Texture.Gray, Texture.Black, Texture.Black };
        public Texture2D DiffuseTexture
        {
            get => Textures[0];
            set => Textures[0] = value;
        }
        public Texture2D NormalTexture
        {
            get => Textures[1];
            set => Textures[1] = value;
        }
        public Texture2D SpecularTexture
        {
            get => Textures[2];
            set => Textures[2] = value;
        }
        public Texture2D IlluminationTexture
        {
            get => Textures[3];
            set => Textures[3] = value;
        }

        public override Shader Shader => factory.GetVarient(
            NormalTexture != Texture.Gray, 
            SpecularTexture != Texture.Black, 
            IlluminationTexture != Texture.Black
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
                TextureRepeat = TextureRepeat
            };
    }
}
