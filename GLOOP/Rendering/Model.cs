using GLOOP.Rendering.Materials;
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
        public bool IsStatic = false;
        public bool IsOccluder = false;


        public Matrix4 BoundingBoxMatrix 
            => Matrix4.CreateScale(VAO.BoundingBox.Size) * Matrix4.CreateTranslation(VAO.BoundingBox.Center) * Transform.Matrix;

        private Matrix4? ModelMatrix;

        public Model(
            VirtualVAO vao,
            Material material,
            Transform? transform = null)
        {
            Transform = transform ?? Transform.Default;
            Material = material;
            VAO = vao;
        }

        public void Render(Matrix4 projectionMatrix, Matrix4 viewMatrix)
        {
            //if (!ModelMatrix.HasValue)
                ModelMatrix = Transform.Matrix;
            Material.SetCameraUniforms(projectionMatrix, viewMatrix, ModelMatrix.Value);
            Material.Commit();
           
            VAO.Draw();
        }

        public void RenderBoundingBox(Matrix4 projectionMatrix, Matrix4 viewMatrix)
        {
            Debugging.Draw.BoundingBox(projectionMatrix, viewMatrix, BoundingBoxMatrix, Vector4.One);
        }

        public Model Clone() => new Model(VAO, Material.Clone(), Transform);
    }
}
