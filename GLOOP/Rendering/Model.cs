using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class Model
    {
        public Transform Transform = Transform.Default;
        public VirtualVAO VAO { get; set; }
        public Material material { get; }

        public Model(
            Transform transform,
            VirtualVAO vao,
            Material material)
        {
            Transform = transform;
            this.material = material;
            VAO = vao;
        }

        public void Render(Matrix4 projectionMatrix, Matrix4 viewMatrix, ref Transform transform)
        {
            var modelMatrix = MathFunctions.CreateModelMatrix(
                transform.Position,
                transform.Rotation,
                transform.Scale
            );
            material.SetCameraUniforms(projectionMatrix, viewMatrix, modelMatrix);
            material.Commit();
           
            VAO.Draw();
        }

        public Model Clone() => new Model(Transform, VAO, material.Clone());
    }
}
