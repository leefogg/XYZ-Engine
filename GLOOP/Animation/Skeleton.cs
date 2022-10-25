using GLOOP.Extensions;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GLOOP.Animation
{
    public class Skeleton
    {
        private readonly Bone RootBone;
        public int TotalBones => BindPose.Length;
        public List<SkeletonAnimation> Animations = new List<SkeletonAnimation>(4);
        public readonly Matrix4 ModelMatrix;

        private readonly Matrix4[] BindPose;

        public Skeleton(Bone rootBone)
        {
            RootBone = rootBone;
        }

        public Skeleton(Assimp.Node rootAssimpNode, IList<Assimp.Bone> assimpBones, IList<Assimp.Animation> animations)
        {
            var skeletonNodes = new List<Assimp.Node>();
            GetAll(skeletonNodes, rootAssimpNode);

            ModelMatrix = rootAssimpNode.Parent.Transform.ToOpenTK();

            BindPose = new Matrix4[assimpBones.Count];

            var boneDict = new Dictionary<string, Bone>(assimpBones.Count);
            for (int i = 0; i < assimpBones.Count; i++)
            {
                var bone = assimpBones[i];
                var node = skeletonNodes[i];

                var boneBindPose = node.Transform.ToOpenTK();
                var newBone = new Bone(bone.Name, i, bone.OffsetMatrix.ToOpenTK());
                BindPose[i] = boneBindPose;

                boneDict.Add(newBone.Name, newBone);
            }

            RootBone = CopyHierarchy(boneDict, rootAssimpNode);

            if ((RootBone.GetAllChildren().Count() - boneDict.Count) > 1)
            {
                var allbones = RootBone.GetAllChildren();
                var missingBones = boneDict.Values.Except(allbones);
                Debug.Fail("Coudn't find some bones in hierarchy");
            }

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

        public void GetModelSpaceTransforms(SkeletonAnimation anim, float timeMs, Span<Matrix4> modelSpaceTransforms)
        {
            var boneTransforms = new Matrix4[BindPose.Length];
            Array.Copy(BindPose, boneTransforms, BindPose.Length);
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
            if (!allBones.TryGetValue(node.Name, out var self))
                return null;

            foreach (var child in node.Children)
                CopyHierarchy(allBones, child);

            foreach (var child in node.Children)
                if (allBones.TryGetValue(child.Name, out var bone))
                    self.Children.Add(bone);

            return self;
        }

        public void MergeAnims(string name)
        {
            if (Animations.Count == 1)
                return;

            var combinedAnim = new SkeletonAnimation(
                Animations.SelectMany(anim => anim.Bones).ToArray(),
                name
            );
            Animations.Add(combinedAnim);
        }

        private void GetAll(List<Assimp.Node> children, Assimp.Node self)
        {
            children.Add(self);
            foreach (var child in self.Children)
                GetAll(children, child);
        }
    }
}
