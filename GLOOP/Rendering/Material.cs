using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public abstract class Material
    {
        protected readonly Shader shader;

        public Matrix4 ModelMatrix = Matrix4.Identity;

        public Material(Shader shader) 
        {
            this.shader = shader;
        }

        public virtual void SetTextures(Texture diffuse, Texture normal, Texture specular, Texture illumination)
        {

        }

        public virtual void SetCameraUniforms(Matrix4 projectionMatrix, Matrix4 viewMatrix, Matrix4 modelMatrix) {
            ModelMatrix = modelMatrix;
        }

        public abstract void Commit();

        public abstract Material Clone();
    }
}
