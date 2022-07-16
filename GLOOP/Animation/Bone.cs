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

        public void GetModelSpaceTransforms(Span<Matrix4> bonePoses, Span<Matrix4> modelSpaceTransforms)
            => GetModelSpaceTransforms(bonePoses, modelSpaceTransforms, Matrix4.Identity);
        private void GetModelSpaceTransforms(Span<Matrix4> bonePoses, Span<Matrix4> modelSpaceTransforms, Matrix4 parentTransformMS)
        {
            var animationTransformMS = bonePoses[Index];
            var msTransform = animationTransformMS * parentTransformMS;

            foreach (var child in Children)
                child.GetModelSpaceTransforms(bonePoses, modelSpaceTransforms, msTransform);

            modelSpaceTransforms[Index] = msTransform;
        }

        public void GetBoneSpaceTransforms(Span<Matrix4> modelSpaceTransforms, Span<Matrix4> boneSpaceTransforms)
        {
            foreach (var child in Children)
                child.GetBoneSpaceTransforms(modelSpaceTransforms, boneSpaceTransforms);

            boneSpaceTransforms[Index] = ModelToBoneSpace * modelSpaceTransforms[Index];
        }

        public override string ToString() => Name;
    }
}
