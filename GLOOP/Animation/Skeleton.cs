using GLOOP.Extensions;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public Skeleton(Assimp.Node rootAssimpNode, IList<Assimp.Bone> assimpBones, IList<Assimp.Animation> animations)
        {
            var skeletonNodes = new List<Assimp.Node>();
            GetAll(skeletonNodes, rootAssimpNode);

            var boneDict = new Dictionary<string, Bone>(assimpBones.Count);
            for (int i = 0; i < assimpBones.Count; i++)
            {
                var bone = assimpBones[i];
                var node = skeletonNodes[i];

                var newBone = new Bone(bone.Name, i, node.Transform.ToOpenTK(), bone.OffsetMatrix.ToOpenTK());

                boneDict.Add(newBone.Name, newBone);
            }

            RootBone = CopyHierarchy(boneDict, rootAssimpNode);

            if (animations != null)
            {
                foreach (var anim in animations)
                {
                    Animations.Add(
                        new SkeletonAnimation(
                            anim.NodeAnimationChannels,
                            boneDict,
                            anim.Name,
                            (float)anim.TicksPerSecond
                        )
                    );
                }
            }
        }

        public void AddAnimation(SkeletonAnimation anim) => Animations.Add(anim);

        public void GetModelSpaceTransforms(SkeletonAnimation anim, float timeMs, Span<Matrix4> modelSpaceTransforms)
        {
            Span<Matrix4> boneTransforms = stackalloc Matrix4[modelSpaceTransforms.Length];
            var identity = Matrix4.Identity;
            foreach (var bone in boneTransforms)
                bone.Copy(identity);
            anim.GetBoneTransforms(boneTransforms, timeMs);

            RootBone.GetModelSpaceTransforms(boneTransforms, modelSpaceTransforms);
        }

        public void GetBoneSpaceTransforms(Span<Matrix4> modelSpaceTransforms, Span<Matrix4> boneSpaceTransforms)
        {
            RootBone.GetBoneSpaceTransforms(modelSpaceTransforms, boneSpaceTransforms);
        }

        public void Render(Rendering.Debugging.DebugLineRenderer lineRenderer, Span<Matrix4> modelSpaceTransforms, Matrix4 modelMatrix)
        {
            RootBone.Render(lineRenderer, modelSpaceTransforms, modelMatrix);
        }

        private Bone CopyHierarchy(IDictionary<string, Bone> allBones, Assimp.Node node)
        {
            var bone = allBones[node.Name];
            foreach (var child in node.Children)
                CopyHierarchy(allBones, child);
            foreach (var child in node.Children)
                bone.Children.Add(allBones[child.Name]);

            return bone;
        }

        public void GetAll(List<Assimp.Node> children, Assimp.Node self)
        {
            children.Add(self);
            foreach (var child in self.Children)
                GetAll(children, child);
        }
    }
}
