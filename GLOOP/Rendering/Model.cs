using GLOOP.Animation;
using GLOOP.Extensions;
using GLOOP.Rendering.Debugging;
using GLOOP.Rendering.Materials;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections;
using System.Collections.Generic;

namespace GLOOP.Rendering
{
    public class Model : Entity
    {
        public VirtualVAO VAO { get; set; }
        public Material Material { get; }
        public bool IsStatic = false;
        public bool IsOccluder = false;

        public bool IsSkinned { get; protected set; }
        public bool SupportsBulkRendering => !IsSkinned;

        public Matrix4 BoundingBoxMatrix
            => Matrix4.CreateScale(VAO.BoundingBox.Size) * Matrix4.CreateTranslation(VAO.BoundingBox.Center) * Transform.Matrix;
        public Box3 BoundingBox => new Box3(-Vector3.One, Vector3.One).Transform(BoundingBoxMatrix);

        public Model(
            VirtualVAO vao,
            Material material,
            DynamicTransform? transform = null) // TODO: Add SetTransform() instead
        {
            Transform = transform ?? DynamicTransform.Default;
            Material = material;
            VAO = vao;
        }

        protected Model(
            VirtualVAO vao, 
            Material material,
            bool isStatic,
            bool isOccluder,
            DynamicTransform? transform = null)
        {
            VAO = vao;
            Material = material;
            IsStatic = isStatic;
            IsOccluder = isOccluder;
        }

        public void Render(PrimitiveType renderMode = PrimitiveType.Triangles)
        {
            Material.SetModelMatrix(Transform.Matrix);
            Material.Commit();
           
            VAO.Draw(renderMode);
        }

        public void RenderBoundingBox()
        {
            Draw.BoundingBox(BoundingBoxMatrix, Vector4.One);
        }

        public virtual Model Clone() => new Model(
            VAO,
            Material.Clone(),
            IsStatic,
            IsOccluder,
            Transform.Clone()
        );
    }
}
