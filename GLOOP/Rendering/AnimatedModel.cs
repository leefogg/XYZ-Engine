using Assimp;
using GLOOP.Animation;
using GLOOP.Rendering.Materials;
using OpenTK.Mathematics;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class AnimatedModel : Model
    {
        private static readonly MemoryPool<Matrix4> BoneTransformHeap = MemoryPool<Matrix4>.Shared;

        private readonly IMemoryOwner<Matrix4> _modelSpaceBoneTransforms, _boneSpaceBoneTransforms;
        public Span<Matrix4> ModelSpaceBoneTransforms => _modelSpaceBoneTransforms.Memory.Span[..Skeleton.TotalBones];
        public Span<Matrix4> BoneSpaceBoneTransforms => _boneSpaceBoneTransforms.Memory.Span[..Skeleton.TotalBones];
        public IList<SkeletonAnimation> Animations;
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
        private float AnimationStart;
        public SkeletonAnimation CurrentAnimation;

        public AnimatedModel(
           VirtualVAO vao,
           Material material,
           Skeleton skeleton,
           IList<SkeletonAnimation> animations,
           DynamicTransform? transform = null
        )
           : this(vao, material, false, false, skeleton, animations, transform)
        {
        }

        protected AnimatedModel(
            VirtualVAO vao, 
            Material material,
            bool isStatic,
            bool isOccluder,
            Skeleton skeleton,
            IList<SkeletonAnimation> animations,
            DynamicTransform? transform = null
        ) 
            : base(vao, material, isStatic, isOccluder, transform)
        {
            AnimationStart = (float)Window.GameMillisecondsElapsed;
            Animations = animations;
            Skeleton = skeleton;
            
            IsSkinned = true;

            System.Diagnostics.Debug.Assert(animations.Count > 0);
            CurrentAnimation = Animations[^1];

            _modelSpaceBoneTransforms = BoneTransformHeap.Rent(Skeleton.TotalBones);
            _boneSpaceBoneTransforms = BoneTransformHeap.Rent(Skeleton.TotalBones);
        }

        public void UpdateBoneTransforms()
        {
            Skeleton.GetModelSpaceTransforms(CurrentAnimation, (float)Window.GameMillisecondsElapsed - AnimationStart, ModelSpaceBoneTransforms);
            Skeleton.GetBoneSpaceTransforms(ModelSpaceBoneTransforms, BoneSpaceBoneTransforms);
        }

        public override Model Clone() => new AnimatedModel(
            VAO,
            Material.Clone(),
            IsStatic,
            IsOccluder,
            Skeleton,
            Animations,
            Transform.Clone()
        );

        ~AnimatedModel()
        {
            _modelSpaceBoneTransforms.Dispose();
            _boneSpaceBoneTransforms.Dispose();
        }
    }
}
