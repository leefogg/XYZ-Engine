﻿using Assimp;
using GLOOP;
using GLOOP.Animation;
using GLOOP.Extensions;
using GLOOP.Util;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AnimationTest
{
    public class Bone
    {
        public string Name { get; set; }
        public int ID { get; set; }
        public List<Bone> Children { get; private set; } = new List<Bone>();
        public TransformTimeline Timeline { get; private set; } = new TransformTimeline(true);
        public Matrix4 ModelToBoneSpace { get; internal set; }
        private Matrix4 InverseBindPose;
        public Matrix4 OffsetFromParent { get; internal set; }
        public Matrix4 ModelSpaceTransform { get; private set; }

        public Bone(string name, int id, Matrix4 offsetFromParent)
        {
            Name = name;
            ID = id;
            OffsetFromParent = offsetFromParent;
        }

        public void AddAnimation(NodeAnimationChannel timeline, float ticksPerSecond)
        {
            // Assumes pos, scale and rot all have keyframes at the same time
            for (int i = 0; i < timeline.PositionKeyCount; i++)
            {
                Debug.Assert(
                    timeline.PositionKeys[i].Time == timeline.ScalingKeys[i].Time
                    && timeline.ScalingKeys[i].Time == timeline.RotationKeys[i].Time,
                    "Mismatching keyframes"
                );

                Timeline.AddKeyframe(
                    (float)(timeline.PositionKeys[i].Time * 1000f * ticksPerSecond),
                    new DynamicTransform(
                        timeline.PositionKeys[i].Value.ToOpenTK(),
                        timeline.ScalingKeys[i].Value.ToOpenTK(),
                        timeline.RotationKeys[i].Value.ToOpenTK()
                    )
                );

            }
        }

        public void CalcInvBindPose(Matrix4 parentBindTransform)
        {
            var bindTransform = parentBindTransform * ModelToBoneSpace;
            InverseBindPose = bindTransform.Inverted();
            foreach (var child in Children)
                child.CalcInvBindPose(bindTransform);
        }

        public void UpdateTransforms(float timeMs, Span<Matrix4> boneTransforms, Matrix4 parentTransformMS)
        {
            var animationTransformMS = Timeline.GetTransformAtTime(timeMs);
            var msTransform = animationTransformMS.Matrix * parentTransformMS;
            foreach (var child in Children)
                child.UpdateTransforms(timeMs, boneTransforms, msTransform);

            ModelSpaceTransform = msTransform;
            boneTransforms[ID] = ModelToBoneSpace * msTransform;
        }

        public override string ToString() => Name;
    }
}
