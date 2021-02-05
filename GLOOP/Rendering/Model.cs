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
        public Material Material { get; }

        public Model(
            Transform transform,
            VirtualVAO vao,
            Material material)
        {
            Transform = transform;
            this.Material = material;
            VAO = vao;
        }

        public void Render(Matrix4 projectionMatrix, Matrix4 viewMatrix, ref Transform transform)
        {
            var modelMatrix = MathFunctions.CreateModelMatrix(
                transform.Position,
                transform.Rotation,
                transform.Scale
            );
            Material.SetCameraUniforms(projectionMatrix, viewMatrix, modelMatrix);
            Material.Commit();
           
            VAO.Draw();
        }

        public Model Clone() => new Model(Transform, VAO, Material.Clone());
    }
}
