using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GLOOP.Animation
{
    public class SkeletonAnimation
    {
        public readonly BoneAnimation[] Bones;

        public readonly string Name;

        public bool Loop
        {
            get => Bones[0].Loop;
            set
            {
                foreach (var bone in Bones)
                    bone.Loop = value;
            }
        }

        public SkeletonAnimation(BoneAnimation[] boneAnimations, string name)
        {
            Bones = boneAnimations;
            Name = name;
        }

        public SkeletonAnimation(IEnumerable<Assimp.NodeAnimationChannel> boneAnims, string name, float ticksPerSecond = 1f)
            : this(boneAnims.Select(anim => new BoneAnimation(anim, ticksPerSecond)).ToArray(), name)
        {
        }

        public void GetBoneTransforms(Span<Matrix4> boneTransforms, float timeMs)
        {
            for (int i = 0; i < Bones.Length; i++)
                boneTransforms[i] = Bones[i].GetTransformAtTime(timeMs);
        }
    }
}
