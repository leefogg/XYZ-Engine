using GLOOP.Rendering.Uniforms;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class DeferredSplatTerrainShader : StaticPixelShader
    {
        private CachedUniformTexture diffuse;
        private Uniform16f modelMatrix;

        public Matrix4 ModelMatrix
        {
            set => modelMatrix.Set(value);
        }

        public DeferredSplatTerrainShader() 
            : base("assets/shaders/Deferred/GeoPass/Terrain.vert", "assets/shaders/Deferred/GeoPass/Terrain.frag", null, "Deferred terrain geometry pass")
        {
            diffuse = new CachedUniformTexture(this, "diffuseTex");
            diffuse.Set(OpenTK.Graphics.OpenGL4.TextureUnit.Texture0);
            modelMatrix = new Uniform16f(this, "ModelMatrix");
        }

        public override int AverageSamplesPerFragment => 1;
        public override int NumOutputTargets => 3;
    }
}
