using Assimp;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Animation
{
    public class SkeletonAnimationDriver
    {
        private static readonly Matrix4[] ModelSpaceTransforms = new Matrix4[Rendering.Globals.Limits.MaxBonesPerModel];
        private static readonly Matrix4[] BoneSpaceTransforms = new Matrix4[Rendering.Globals.Limits.MaxBonesPerModel];
        private readonly Skeleton Skeleton;
        public float TimeMs = 0;

        public SkeletonAnimationDriver(Skeleton skeleton)
        {
            Skeleton = skeleton;

            if (skeleton.TotalBones > Rendering.Globals.Limits.MaxBonesPerModel)
                Console.WriteLine("Skeleton has more than the maximum number of bones"); // Move this
        }

        public void PrepareMatrixes(SkeletonAnimation anim)
        {
            GetBoneMatrixes(anim, Skeleton, TimeMs);
        }

        public static void GetBoneMatrixes(SkeletonAnimation animation, Skeleton skeleton, float timeMs)
        {
            skeleton.GetModelSpaceTransforms(animation, timeMs, ModelSpaceTransforms);
            var modelspaceTransforms = ModelSpaceTransforms[..skeleton.TotalBones];
            skeleton.GetBoneSpaceTransforms(modelspaceTransforms, BoneSpaceTransforms);
        }
        public static Span<Matrix4> GetModelspaceTransforms(int numBones) => ModelSpaceTransforms[..numBones];
        public static Span<Matrix4> GetBonespaceTransforms(int numBones) => BoneSpaceTransforms[..numBones];

        public void Update(float deltaMs)
        {
            TimeMs += deltaMs;
        }
    }
}
