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
        private static readonly SingleColorMaterial boundingBoxMaterial = new SingleColorMaterial(Shader.SingleColorShader) { Color = new Vector4(1) };

        public Transform Transform = Transform.Default;
        public VirtualVAO VAO { get; set; }
        public Material Material { get; }

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
            var bb = VAO.BoundingBox;
            var modelsMatrix = Transform.Matrix;
            var boundingBoxModelMatrix = Matrix4.CreateScale(bb.Size) * Matrix4.CreateTranslation(bb.Center) * modelsMatrix;
            boundingBoxMaterial.SetCameraUniforms(projectionMatrix, viewMatrix, boundingBoxModelMatrix);
            boundingBoxMaterial.Commit();
            Primitives.Cube.Draw(PrimitiveType.Lines);
        }

        public Model Clone() => new Model(VAO, Material.Clone(), Transform);
    }
}
