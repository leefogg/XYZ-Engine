using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Materials
{
    public class SingleColorMaterial : Material
    {
        public Matrix4 ProjectionMatrix, ViewMatrix;
        public Vector4 Color;
        private SingleColorShader shader;

        public override Shader Shader => shader;

        public SingleColorMaterial(SingleColorShader shader)
        {
            this.shader = shader;
        }

        public override void SetCameraUniforms(Matrix4 projectionMatrix, Matrix4 viewMatrix, Matrix4 modelMatrix)
        {
            ProjectionMatrix = projectionMatrix;
            ViewMatrix = viewMatrix;
            ModelMatrix = modelMatrix;
        }

        public override void Commit()
        {
            shader.Use();

            shader.ProjectionMatrix = ProjectionMatrix;
            shader.ViewMatrix = ViewMatrix;
            shader.ModelMatrix = ModelMatrix;
            shader.Color = Color;
        }

        public override Material Clone()
        {
            return new SingleColorMaterial(shader)
            {
                Color = Color
            };
        }
    }
}
