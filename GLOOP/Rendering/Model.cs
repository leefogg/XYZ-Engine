using GLOOP.Extensions;
using GLOOP.Rendering.Materials;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class Model : Entity
    {
        public override Transform Transform { get; set; } = Transform.Default;
        public VirtualVAO VAO { get; set; }
        public Material Material { get; }
        public bool IsStatic = false;
        public bool IsOccluder = false;

        public Matrix4 BoundingBoxMatrix
            => Matrix4.CreateScale(VAO.BoundingBox.Size) * Matrix4.CreateTranslation(VAO.BoundingBox.Center) * Transform.Matrix;
        public Box3 BoundingBox => new Box3(new Vector3(-1f), new Vector3(1f)).Transform(BoundingBoxMatrix);


        public Model(
            VirtualVAO vao,
            Material material,
            Transform? transform = null)
        {
            Transform = transform ?? Transform.Default;
            Material = material;
            VAO = vao;
        }

        public void Render(PrimitiveType renderMode = PrimitiveType.Triangles)
        {
            Material.SetModelMatrix(Transform.Matrix);
            Material.Commit();
           
            VAO.Draw(renderMode);
        }

        public void RenderBoundingBox()
        {
            Debugging.Draw.BoundingBox(BoundingBoxMatrix, Vector4.One);
        }

        public Model Clone() => new Model(VAO, Material.Clone(), Transform);
    }
}
