using GLOOP.Animation;
using GLOOP.Extensions;
using GLOOP.Rendering.Debugging;
using GLOOP.Rendering.Materials;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;

namespace GLOOP.Rendering
{
    public class Model : Entity
    {
        private static readonly Matrix4[] ModelSpaceTransforms = new Matrix4[Globals.Limits.MaxBonesPerModel];
        private static readonly Matrix4[] BoneSpaceTransforms = new Matrix4[Globals.Limits.MaxBonesPerModel];

        public VirtualVAO VAO { get; set; }
        public Material Material { get; }
        public bool IsStatic = false;
        public bool IsOccluder = false;
        private Skeleton _skeleton;
        public Skeleton Skeleton
        {
            get => _skeleton;
            set
            {
                _skeleton = value;
                if (Material is ISkinnableMaterial mat)
                    mat.IsSkinned = Skeleton != null;
            }
        }
        public SkeletonAnimationSet Animations { get; set; }
        public SkeletonAnimationDriver AnimationDriver { get; set; }

        public bool IsSkinned => Skeleton != null;
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

        private Model(
            VirtualVAO vao, 
            Material material,
            bool isStatic,
            bool isOccluder,
            Skeleton skeleton, 
            SkeletonAnimationSet animations, 
            SkeletonAnimationDriver animationDriver,
            DynamicTransform? transform = null)
        {
            VAO = vao;
            Material = material;
            IsStatic = isStatic;
            IsOccluder = isOccluder;
            Skeleton = skeleton;
            Animations = animations;
            AnimationDriver = animationDriver;
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

        public Span<Matrix4> GetModelSpaceBoneTransforms(SkeletonAnimation animation, float timeMs)
        {
            Skeleton.GetModelSpaceTransforms(animation, timeMs, ModelSpaceTransforms);
            return ModelSpaceTransforms[..Skeleton.TotalBones];
        }

        public Span<Matrix4> GetBoneSpaceBoneTransforms(Span<Matrix4> modelspaceTransforms)
        {
            Skeleton.GetBoneSpaceTransforms(modelspaceTransforms, BoneSpaceTransforms);
            return BoneSpaceTransforms[..Skeleton.TotalBones];
        }

        public Model Clone() => new Model(
            VAO,
            Material.Clone(),
            IsStatic,
            IsOccluder,
            Skeleton,
            Animations,
            AnimationDriver,
            Transform.Clone()
        );
    }
}
