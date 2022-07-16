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

        public SkeletonAnimation(BoneAnimation[] boneAnimations, string name)
        {
            Bones = boneAnimations;
            Name = name;
        }

        public SkeletonAnimation(IEnumerable<Assimp.NodeAnimationChannel> boneAnims, string name, float ticksPerSecond = 1f)
            : this(boneAnims.Select(anim => new BoneAnimation(anim, ticksPerSecond)).ToArray(), name)
        {
        }
    }
}
