using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class SingleColorMaterial : Material
    {
        public Matrix4 ProjectionMatrix, ViewMatrix;
        public Vector4 Color;

        public SingleColorMaterial(SingleColorShader shader) : base(shader)
        {

        }

        public override void SetCameraUniforms(Matrix4 projectionMatrix, Matrix4 viewMatrix, Matrix4 modelMatrix)
        {
            ProjectionMatrix = projectionMatrix;
            ViewMatrix = viewMatrix;
            ModelMatrix = modelMatrix;
        }

        public override void Commit()
        {
            var shader = (SingleColorShader)base.shader;
            shader.Use();

            shader.ProjectionMatrix = ProjectionMatrix;
            shader.ViewMatrix = ViewMatrix;
            shader.ModelMatrix = ModelMatrix;
            shader.Color = Color;
        }

        public override Material Clone()
        {
            return new SingleColorMaterial((SingleColorShader)shader)
            {
                Color = Color
            };
        }
    }
}
