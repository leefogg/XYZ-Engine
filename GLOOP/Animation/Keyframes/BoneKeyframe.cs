using GLOOP.Util;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GLOOP.Animation.Keyframes
{
    public class BoneKeyframe : Keyframe<Matrix4>
    {
        public BoneTransform Transform;
        public override Matrix4 Value => Transform.Matrix;

        public BoneKeyframe(float timeMs, BoneTransform transform) : base(timeMs)
        {
            Transform = transform;
        }


        public override Matrix4 Tween(Keyframe<Matrix4> other, float percent)
        {
            var otherBone = other as BoneKeyframe;
            return MathFunctions.Tween(Transform.Matrix, otherBone.Transform.Matrix, percent);
        }

        public override string ToString() => Value.ToString();
    }
}
