using GLOOP.Rendering;
using GLOOP.Tests.Assets.Shaders;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Tests.Assets.Shaders
{
    public class FrustumMaterial : Material
    {
        public Matrix4 ProjectionMatrix, ViewMatrix;
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

            shader.ProjectionMatrix = ProjectionMatrix;
            shader.ViewMatrix = ViewMatrix;
            shader.ModelMatrix = ModelMatrix;
            shader.Scale = Scale;
            shader.AspectRatio = AspectRatio;
        }

        public override void SetCameraUniforms(Matrix4 projectionMatrix, Matrix4 viewMatrix, Matrix4 modelMatrix)
        {
            ProjectionMatrix = projectionMatrix;
            ViewMatrix = viewMatrix;
            ModelMatrix = modelMatrix;
        }
    }
}
