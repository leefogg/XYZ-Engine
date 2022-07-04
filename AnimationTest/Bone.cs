using Assimp;
using GLOOP;
using GLOOP.Animation;
using GLOOP.Extensions;
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
        public DynamicTransform InvBindPose { get; internal set; }
        public DynamicTransform CurrentTransform { get; private set; }

        public Bone(string name)
        {
            Name = name;
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

        public void UpdateTransforms(float timeMs, ref Matrix4[] boneTransforms, in Matrix4 parentTransform)
        {
            var localTransform = Timeline.GetTransformAtTime(timeMs).Matrix;
            var currentTransform = localTransform;
            currentTransform *= parentTransform;
            foreach (var child in Children)
                child.UpdateTransforms(timeMs, ref boneTransforms, currentTransform);
            boneTransforms[ID] = localTransform * InvBindPose.Matrix;
        }

        public override string ToString() => Name;
    }
}
