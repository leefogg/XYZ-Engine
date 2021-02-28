using GLOOP.Rendering.Uniforms;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
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
        private int numSamples = 1;

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

            foreach (var value in defines.Values)
                if (value == "1")
                    numSamples++;
        }

        public override int AverageSamplesPerFragment => numSamples;
        public override int NumOutputTargets => 3;
    }
}