using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Animation
{
    public class Skeleton
    {
        public readonly Bone RootBone;

        public List<SkeletonAnimation> Animations = new List<SkeletonAnimation>(4);

        public Skeleton(Bone rootBone)
        {
            RootBone = rootBone;
        }

        public void AddAnimation(SkeletonAnimation anim) => Animations.Add(anim);

        public Matrix4[] GetModelSpaceTransforms(SkeletonAnimation anim, float timeMs)
        {
            var modelSpaceTransforms = new Matrix4[RootBone.TotalBones];

            RootBone.GetModelSpaceTransforms(anim, timeMs, modelSpaceTransforms);

            return modelSpaceTransforms;
        }

        public Matrix4[] GetBoneSpaceTransforms(Span<Matrix4> modelSpaceTransforms)
        {
            var boneSpaceTransforms = new Matrix4[modelSpaceTransforms.Length];

            RootBone.GetBoneSpaceTransforms(modelSpaceTransforms, boneSpaceTransforms);

            return boneSpaceTransforms;
        }
    }
}
