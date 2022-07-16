using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Animation
{
    public class Skeleton
    {
        private readonly Bone RootBone;

        private int? totalBones;
        public int TotalBones => totalBones ??= RootBone.TotalBones;

        public List<SkeletonAnimation> Animations = new List<SkeletonAnimation>(4);

        public Skeleton(Bone rootBone)
        {
            RootBone = rootBone;
        }

        public void AddAnimation(SkeletonAnimation anim) => Animations.Add(anim);

        public void GetModelSpaceTransforms(SkeletonAnimation anim, float timeMs, Span<Matrix4> modelSpaceTransforms)
        {
            Span<Matrix4> boneTransforms = stackalloc Matrix4[modelSpaceTransforms.Length];

            anim.GetBoneTransforms(boneTransforms, timeMs);

            RootBone.GetModelSpaceTransforms(boneTransforms, modelSpaceTransforms);
        }

        public void GetBoneSpaceTransforms(Span<Matrix4> modelSpaceTransforms, Span<Matrix4> boneSpaceTransforms)
        {
            RootBone.GetBoneSpaceTransforms(modelSpaceTransforms, boneSpaceTransforms);
        }
    }
}
