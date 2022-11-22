using GLOOP.Animation;
using GLOOP.Rendering.Materials;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class AnimatedModel : Model
    {
        private static readonly Matrix4[] ModelSpaceTransforms = new Matrix4[Globals.Limits.MaxBonesPerModel];
        private static readonly Matrix4[] BoneSpaceTransforms = new Matrix4[Globals.Limits.MaxBonesPerModel];

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
            AnimationStart = Window.FrameMillisecondsElapsed;
            Animations = animations;
            Skeleton = skeleton;

            IsSkinned = true;
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

        public override Model Clone() => new AnimatedModel(
            VAO,
            Material.Clone(),
            IsStatic,
            IsOccluder,
            Skeleton,
            Animations,
            Transform.Clone()
        );
    }
}
