using GLOOP.Util;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GLOOP.Animation.Keyframes
{
    public class Vector3Keyframe : Keyframe<Vector3>
    {
        public readonly Vector3 Vector;
        public override Vector3 Value => Vector;

        public Vector3Keyframe(float timeMs, Vector3 vector) : base(timeMs)
        {
            Vector = vector;
        }

        public override Vector3 Tween(Keyframe<Vector3> other, float percent)
        {
            var otherBone = other as Vector3Keyframe;
            return MathFunctions.Tween(Vector, otherBone.Vector, percent);
        }

        public override string ToString() => Value.ToString();
    }
}
