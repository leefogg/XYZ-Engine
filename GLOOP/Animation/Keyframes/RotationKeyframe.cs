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

        public override Quaternion Tween(Keyframe<Quaternion> other, float timeMs)
        {
            var otherBone = other as RotationKeyframe;

            var percent = (float)MathFunctions.Map(timeMs, TimeMs, other.TimeMs, 0, 1);
            return MathFunctions.Tween(Rotation, otherBone.Rotation, percent);
        }
    }
}
