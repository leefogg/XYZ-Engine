using GLOOP.Animation.Keyframes;
using GLOOP.Extensions;
using GLOOP.Util;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GLOOP.Animation
{
    public class BoneAnimation
    {
        private readonly Timeline<RotationKeyframe, Quaternion> rotationKeyframes;
        private readonly Timeline<Vector3Keyframe,  Vector3>    positionKeyframes;

        public readonly int BoneIndex;
        public IReadonlyTimeline<RotationKeyframe, Quaternion> RotationKeyframes => rotationKeyframes;
        public IReadonlyTimeline<Vector3Keyframe,  Vector3> PositionKeyframes => positionKeyframes;
        public bool Loop
        {
            get => rotationKeyframes.Loop;
            set
            {
                rotationKeyframes.Loop = value;
                positionKeyframes.Loop = value;
            }
        }

        public BoneAnimation(int boneIndex, int numRotationKeys, int numPositionKeys)
        {
            rotationKeyframes = new Timeline<RotationKeyframe, Quaternion>(numRotationKeys);
            positionKeyframes = new Timeline<Vector3Keyframe, Vector3>(numPositionKeys);

            BoneIndex = boneIndex;
        }

        public BoneAnimation(int boneIndex, Assimp.NodeAnimationChannel bone, float ticksPerSecond = 1f)
            : this(boneIndex, bone.RotationKeyCount, bone.PositionKeyCount)
        {
            BoneIndex = boneIndex;

            positionKeyframes = new Timeline<Vector3Keyframe, Vector3>(
                bone.PositionKeys.Select(keyframe => new Vector3Keyframe((float)(keyframe.Time * 1000f * ticksPerSecond), keyframe.Value.ToOpenTK()))
            );

            rotationKeyframes = new Timeline<RotationKeyframe, Quaternion>(
                bone.RotationKeys.Select(keyframe => new RotationKeyframe((float)(keyframe.Time * 1000f * ticksPerSecond), keyframe.Value.ToOpenTK().Inverted()))
            );
        }

        public Matrix4 GetTransformAtTime(float timeMs)
        {
            var pos = positionKeyframes.GetValueAtTime(timeMs);
            var rot = rotationKeyframes.GetValueAtTime(timeMs);
            return MathFunctions.CreateModelMatrix(pos, rot, Vector3.One);
        }
    }
}
