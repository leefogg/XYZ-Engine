using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public abstract class Material
    {
        public abstract Shader Shader { get; }

        public Matrix4 ModelMatrix = Matrix4.Identity;

        public virtual void SetTextures(Texture2D diffuse, Texture2D normal, Texture2D specular, Texture2D illumination)
        {

        }

        public virtual void SetCameraUniforms(Matrix4 projectionMatrix, Matrix4 viewMatrix, Matrix4 modelMatrix) {
            ModelMatrix = modelMatrix;
        }

        public abstract void Commit();

        public abstract Material Clone();
    }
}
