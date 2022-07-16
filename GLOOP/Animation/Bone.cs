using Assimp;
using GLOOP;
using GLOOP.Animation.Keyframes;
using GLOOP.Extensions;
using GLOOP.Util;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GLOOP.Animation
{
    public class Bone
    {
        public string Name { get; private set; }
        public int Index { get; private set; }
        public List<Bone> Children { get; private set; } = new List<Bone>();
        public Timeline<BoneKeyframe, Matrix4> Timeline { get; private set; }
        public Matrix4 ModelToBoneSpace { get; private set; }
        public Matrix4 OffsetFromParent { get; private set; }

        public int TotalBones => TotalChildren();

        public Bone(string name, int id, Matrix4 offsetFromParent, Matrix4 modelToBoneSpace)
        {
            Name = name;
            Index = id;
            OffsetFromParent = offsetFromParent;
            ModelToBoneSpace = modelToBoneSpace;
        }

        public void AddAnimation(NodeAnimationChannel timeline, float ticksPerSecond)
        {
            Timeline = new Timeline<BoneKeyframe, Matrix4>(true);
            // Assumes pos, scale and rot all have keyframes at the same time
            for (int i = 0; i < timeline.PositionKeyCount; i++)
            {
                Debug.Assert(
                    timeline.PositionKeys[i].Time == timeline.ScalingKeys[i].Time
                    && timeline.ScalingKeys[i].Time == timeline.RotationKeys[i].Time,
                    "Mismatching keyframes"
                );

                Timeline.AddKeyframe(
                    new BoneKeyframe(
                        (float)(timeline.PositionKeys[i].Time * 1000f * ticksPerSecond),
                        new BoneTransform(
                            timeline.PositionKeys[i].Value.ToOpenTK(),
                            timeline.RotationKeys[i].Value.ToOpenTK()
                        )
                    )
                );
            }
        }

        public int TotalChildren()
        {
            var total = 0;
            TotalChildren(ref total);
            return total;
        }
        private void TotalChildren(ref int count)
        {
            count++;
            foreach (var child in Children)
                child.TotalChildren(ref count);
        }

        public void GetModelSpaceTransforms(SkeletonAnimation anim, float timeMs, Span<Matrix4> modelSpaceTransforms)
            => GetModelSpaceTransforms(anim, timeMs, modelSpaceTransforms, Matrix4.Identity);
        private void GetModelSpaceTransforms(SkeletonAnimation anim, float timeMs, Span<Matrix4> modelSpaceTransforms, Matrix4 parentTransformMS)
        {
            var animationTransformMS = anim.Bones[Index].GetTransformAtTime(timeMs);
            var msTransform = animationTransformMS * parentTransformMS;

            foreach (var child in Children)
                child.GetModelSpaceTransforms(anim, timeMs, modelSpaceTransforms, msTransform);

            modelSpaceTransforms[Index] = msTransform;
        }

        public void GetBoneSpaceTransforms(Span<Matrix4> modelSpaceTransforms, Span<Matrix4> boneSpaceTransforms)
        {
            foreach (var child in Children)
                child.GetBoneSpaceTransforms(modelSpaceTransforms, boneSpaceTransforms);

            boneSpaceTransforms[Index] = ModelToBoneSpace * modelSpaceTransforms[Index];
        }

        //public void UpdateTransforms(float timeMs, Span<Matrix4> boneSpaceTransforms, Span<Matrix4> modelSpaceTransforms, Matrix4 parentTransformMS)
        //{
        //    var animationTransformMS = Timeline?.GetValueAtTime(timeMs) ?? OffsetFromParent;
        //    var msTransform = animationTransformMS * parentTransformMS;

        //    foreach (var child in Children)
        //        child.UpdateTransforms(timeMs, boneSpaceTransforms, modelSpaceTransforms, msTransform);

        //    modelSpaceTransforms[Index] = msTransform;
        //    boneSpaceTransforms[Index] = ModelToBoneSpace * msTransform;
        //}

        public override string ToString() => Name;
    }
}
