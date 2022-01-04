using GLOOP.Rendering;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Materials
{
    public class FrustumMaterial : Material
    {
        public Vector3 Scale;
        public float AspectRatio;
        private FrustumShader shader;

        public override Shader Shader => shader;

        public override Material Clone()
        {
            return new FrustumMaterial(shader)
            {
                Scale = Scale,
                AspectRatio = AspectRatio
            };
        }

        public FrustumMaterial(FrustumShader shader)
        {
            this.shader = shader;
        }

        public override void Commit()
        {
            shader.Use();
            shader.ModelMatrix = ModelMatrix;
            shader.Scale = Scale;
            shader.AspectRatio = AspectRatio;
        }
    }
}
