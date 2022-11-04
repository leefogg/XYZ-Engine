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

        // TODO: Virtual start and stop time fields

        public SkeletonAnimation(BoneAnimation[] boneAnimations, string name)
        {
            Bones = boneAnimations;
            Name = name;
        }

        public SkeletonAnimation(IEnumerable<Assimp.NodeAnimationChannel> boneAnims, IDictionary<string, Bone> bones, string name, float ticksPerSecond = 1f)
            : this(ExtractAnimations(boneAnims, bones, ticksPerSecond), name)
        {
        }

        private static BoneAnimation[] ExtractAnimations(IEnumerable<Assimp.NodeAnimationChannel> boneAnims, IDictionary<string, Bone> bones, float ticksPerSecond)
        {
            //return boneAnims.Select(anim => new BoneAnimation(bones[anim.NodeName].Index, anim, ticksPerSecond)).ToArray();

            var anims = new List<BoneAnimation>();

            foreach (var boneAnim in boneAnims)
                if (bones.TryGetValue(boneAnim.NodeName, out var bone))
                    anims.Add(new BoneAnimation(bone.Index, boneAnim, ticksPerSecond));

            return anims.ToArray();
        }

        public void GetBoneTransforms(Span<Matrix4> boneTransforms, float timeMs)
        {
            foreach (var boneAnim in Bones)
                boneTransforms[boneAnim.BoneIndex] = boneAnim.GetTransformAtTime(timeMs);
        }

        public SkeletonAnimation CombineWith(SkeletonAnimation anim, string name = null)
        {
            return new SkeletonAnimation(
                Bones.Concat(anim.Bones).ToArray(),
                name ?? (Name + anim.Name)
            );
        }

        public override string ToString() => Name;
    }
}
