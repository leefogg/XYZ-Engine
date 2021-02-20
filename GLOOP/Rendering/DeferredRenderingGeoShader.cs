using GLOOP.Rendering.Uniforms;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class DeferredRenderingGeoShader : DynamicPixelShader
    {
        private Uniform2f textureOffset, textureRepeat;
        private Uniform3f illuminationColor, albedoColourTint;
        private Uniform1b hasWorldspaceUVs;
        //private Uniform1ui diffuseSlice, normalSlice, specularSlice, illumSlice;
        private Uniform16f modelMatrix;

        public Vector2 TextureOffset {
            set => textureOffset.Set(value);
        }
        public Vector2 TextureRepeat {
            set => textureRepeat.Set(value);
        }
        public bool HasWorldpaceUVs {
            set => hasWorldspaceUVs.Set(value);
        }
        public Vector3 IlluminationColor {
            set => illuminationColor.Set(value);
        }
        public Vector3 AlbedoColourTint
        {
            set => albedoColourTint.Set(value);
        }
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
        /*
        public ushort DiffuseTextureSlice
        {
            set => diffuseSlice.Set(value);
        }
        public ushort NormalTextureSlice
        {
            set => normalSlice.Set(value);
        }
        public ushort SpecularTextureSlice
        {
            set => specularSlice.Set(value);
        }
        public ushort IlluminationTextureSlice
        {
            set => illumSlice.Set(value);
        }
        */
        public Matrix4 ModelMatrix {
            set => modelMatrix.Set(value);
        }

        public DeferredRenderingGeoShader() 
            : base("assets/shaders/Deferred/GeoPass/VertexShader.vert", "assets/shaders/Deferred/GeoPass/FragmentShader.frag", name: "Deferred geometry pass")
        {
            Reload();
        }

        protected override void Reload()
        {
            textureOffset = new CachedUniform2f(this, "textureOffset");
            textureRepeat = new CachedUniform2f(this, "textureRepeat");
            hasWorldspaceUVs = new CachedUniform1b(this, "isWorldSpaceUVs");
            illuminationColor = new CachedUniform3f(this, "illuminationColor");
            albedoColourTint = new CachedUniform3f(this, "albedoColourTint");
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
            /*
            diffuseSlice = new CachedUniform1ui(this, "diffuseSlice");
            normalSlice = new CachedUniform1ui(this, "normalSlice");
            specularSlice = new CachedUniform1ui(this, "specularSlice");
            illumSlice = new CachedUniform1ui(this, "illumSlice");
            */

            modelMatrix = new CachedUniform16f(this, "ModelMatrix");
        }

        public override int AverageSamplesPerFragment => 4;
        public override int NumOutputTargets => 4;
    }
}