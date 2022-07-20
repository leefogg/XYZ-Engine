using GLOOP.Util;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GLOOP.Animation.Keyframes
{
    public class RotationKeyframe : Keyframe<Quaternion>
    {
        private readonly Quaternion Rotation;

        public override Quaternion Value => Rotation;

        public RotationKeyframe(float timeMs, Quaternion rotation) : base(timeMs)
        {
            Rotation = rotation;
        }

        public override Quaternion Tween(Keyframe<Quaternion> other, float percent)
        {
            var otherBone = other as RotationKeyframe;
            return MathFunctions.Tween(Rotation, otherBone.Rotation, percent);
        }

        public override string ToString() => Value.ToString();
    }
}
