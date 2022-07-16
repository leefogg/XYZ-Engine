using GLOOP.Animation.Keyframes;
using GLOOP.Extensions;
using GLOOP.Util;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Animation
{
    public class BoneAnimation
    {
        private readonly Timeline<RotationKeyframe, Quaternion> RotationKeyframes;
        private readonly Timeline<Vector3Keyframe,  Vector3>    PositionKeyframes;
        public readonly int BoneIndex;

        public BoneAnimation(int boneIndex, int numRotationKeys, int numPositionKeys)
        {
            RotationKeyframes = new Timeline<RotationKeyframe, Quaternion>(numRotationKeys);
            PositionKeyframes = new Timeline<Vector3Keyframe, Vector3>(numPositionKeys);

            BoneIndex = boneIndex;
        }

        public BoneAnimation(int boneIndex, Assimp.NodeAnimationChannel bone, float ticksPerSecond = 1f) 
            : this(boneIndex, bone.RotationKeyCount, bone.PositionKeyCount)
        {
            foreach (var position in bone.PositionKeys)
                AddPositionKeyframe((float)(position.Time * 1000 * ticksPerSecond), position.Value.ToOpenTK());

            foreach (var rot in bone.RotationKeys)
                AddRotationKeyframe((float)(rot.Time * 1000 * ticksPerSecond), rot.Value.ToOpenTK().Inverted());
        }

        public bool Loop
        {
            get => RotationKeyframes.Loop;
            set
            {
                RotationKeyframes.Loop = value;
                PositionKeyframes.Loop = value;
            }
        }

        public void AddRotationKeyframe(float timeMs, Quaternion rotation)
        {
            RotationKeyframes.AddKeyframe(new RotationKeyframe(timeMs, rotation));
        }

        public void AddPositionKeyframe(float timeMs, Vector3 position)
        {
            PositionKeyframes.AddKeyframe(new Vector3Keyframe(timeMs, position));
        }

        public Matrix4 GetTransformAtTime(float timeMs)
        {
            var pos = PositionKeyframes.GetValueAtTime(timeMs);
            var rot = RotationKeyframes.GetValueAtTime(timeMs);
            return MathFunctions.CreateModelMatrix(pos, rot, Vector3.One);
        }
    }
}
