using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class FullbrightMaterial : Material
    {
        public Matrix4 ProjectionMatrix, ViewMatrix;
        public Texture diffuse;
        private FullbrightShader shader;

        public override Shader Shader => shader;

        public FullbrightMaterial(FullbrightShader shader)
        {
            this.shader = shader;
        }

        public override void SetTextures(Texture diffuse, Texture normal, Texture specular, Texture illumination)
        {
            this.diffuse = diffuse;
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
            shader.DiffuseTexture = diffuse.BindlessHandle;
        }

        public override Material Clone()
        {
            return new FullbrightMaterial(shader)
            {
                diffuse = diffuse
            };
        }
    }
}
