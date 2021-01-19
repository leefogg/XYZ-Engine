using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class Renderable
    {
        public VirtualVAO VAO { get; set; }
        public Material material { get; }

        public Renderable(
            VirtualVAO vao,
            Material material)
        {
            this.material = material;
            VAO = vao;
        }

        public void Render(Matrix4 projectionMatrix, Matrix4 viewMatrix, Matrix4 modelMatrix)
        {
            material.SetCameraUniforms(projectionMatrix, viewMatrix, modelMatrix);
            material.Commit();
           
            VAO.Draw();
        }
    }
}
