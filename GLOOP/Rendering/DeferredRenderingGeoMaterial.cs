using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
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
            => new DeferredRenderingGeoMaterial((DeferredRenderingGeoShader)shader)
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
