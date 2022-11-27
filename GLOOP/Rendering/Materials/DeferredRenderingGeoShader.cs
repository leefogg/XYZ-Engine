using GLOOP.Rendering.Uniforms;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Materials
{
    public class DeferredRenderingGeoShader : StaticPixelShader
    {
#if BINDLESSTEXTURES
        private UniformBindlessTexture diffuse, normal, specular, illumination;
        public ulong DiffuseTexture {
            set => diffuse.Set(value);
        }
        public ulong NormalTexture
        {
            set => normal.Set(value);
        }
        public ulong SpecularTexture {
            set => specular.Set(value);
        }
        public ulong IlluminationTexture {
            set => illumination.Set(value);
        }
#else
        private CachedUniformTexture diffuse, normal, specular, illumination;
        private Uniform16f modelMatrix;
        private Uniform3f illumColor, albedoColor;
        private Uniform2f textureRepeat, textureOffset;
        private Uniform1f normalStrength;
        private Uniform1b isWorldSpaceUvs;
        public TextureUnit DiffuseTexture
        {
            set => diffuse.Set(value);
        }
        public TextureUnit NormalTexture
        {
            set => normal.Set(value);
        }
        public TextureUnit SpecularTexture
        {
            set => specular.Set(value);
        }
        public TextureUnit IlluminationTexture
        {
            set => illumination.Set(value);
        }
#endif
        public Matrix4 ModelMatrix
        {
            set => modelMatrix.Set(value);
        }
        public Vector3 AlbedoColor
        {
            set => albedoColor.Set(value);
        }
        public Vector3 IlluminationColor
        {
            set => illumColor.Set(value);
        }
        public Vector2 TextureRepeat
        {
            set => textureRepeat.Set(value);
        }
        public Vector2 TextureOffset
        {
            set => textureOffset.Set(value);
        }
        public float NormalStrength
        {
            set => normalStrength.Set(value);
        }
        public bool IsWorldSpaceUvs
        {
            set => isWorldSpaceUvs.Set(value);
        }


        public DeferredRenderingGeoShader(IDictionary<string, string> defines)
            : base("assets/shaders/Deferred/GeoPass/VertexShader.vert", "assets/shaders/Deferred/GeoPass/FragmentShader.frag", defines, "Deferred geometry pass")
        {

#if BINDLESSTEXTURES
            diffuse = new UniformBindlessTexture(this, "diffuseTex");
            normal = new UniformBindlessTexture(this, "normalTex");
            specular = new UniformBindlessTexture(this, "specularTex");
            illumination = new UniformBindlessTexture(this, "illumTex");
#else
            diffuse = new CachedUniformTexture(this, "diffuseTex");
            normal = new CachedUniformTexture(this, "normalTex");
            specular = new CachedUniformTexture(this, "specularTex");
            illumination = new CachedUniformTexture(this, "illumTex");
#endif

            diffuse.Set(TextureUnit.Texture0);
            normal.Set(TextureUnit.Texture1);
            specular.Set(TextureUnit.Texture2);
            illumination.Set(TextureUnit.Texture3);
        }
    }
}