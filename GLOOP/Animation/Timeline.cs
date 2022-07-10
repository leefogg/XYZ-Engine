using GLOOP.Util;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GLOOP.Animation
{
    public class TransformTimeline
    {
        public bool Loop = false;

        private class Keyframe
        {
            public float Time;
            public DynamicTransform Transform;

            public Keyframe(float time, DynamicTransform transform)
            {
                Time = time;
                Transform = transform;
            }

            public override string ToString() => Time.ToString();
        }

        private readonly List<Keyframe> Keyframes;

        public TransformTimeline(bool loop)
        {
            Loop = loop;
            Keyframes = new List<Keyframe>();
        }

        public TransformTimeline(int initialKeyframes = 16)
        {
            Keyframes = new List<Keyframe>();
        }
        public TransformTimeline(float[] timeMs, DynamicTransform[] transforms)
            : this(timeMs.Length)
        {
            for (int i = 0; i < timeMs.Length; i++)
                AddKeyframe(timeMs[i], transforms[i]);
        }

        
        public void AddKeyframe(float timeMs, DynamicTransform transform)
        {
            Keyframes.Add(new Keyframe(timeMs, transform));
        }

        public Vector3 GetPositionAtTime(float timeMs)
        {
            var index = FindKeyframeAfterTime(timeMs);
            if (index == -1)
                return Vector3.Zero;

            var currentPos = Keyframes[index].Transform.Position;
            if (index == Keyframes.Count - 1)
                return Loop ? GetPositionAtTime(timeMs % Keyframes[^1].Time) : currentPos;

            var nextPos = Keyframes[index + 1].Transform.Position;
            return MathFunctions.Map(
                timeMs,
                Keyframes[index].Time,
                Keyframes[index + 1].Time, 
                currentPos, 
                nextPos
            );
        }

        public Vector3 GetScaleAtTime(float timeMs)
        {
            var index = FindKeyframeAfterTime(timeMs);
            if (index == -1)
                return Vector3.Zero;

            var currentScale = Keyframes[index].Transform.Scale;
            if (index == Keyframes.Count - 1)
                return Loop ? GetScaleAtTime(timeMs % Keyframes[^1].Time) : currentScale;

            var nextScale = Keyframes[index + 1].Transform.Scale;
            return MathFunctions.Map(
                timeMs,
                Keyframes[index].Time,
                Keyframes[index + 1].Time,
                currentScale,
                nextScale
            );
        }

        public Quaternion GetRotationAtTime(float timeMs)
        {
            var index = FindKeyframeAfterTime(timeMs);
            if (index == -1)
                return new Quaternion();

            var currentRot = Keyframes[index].Transform.Rotation;
            if (index == Keyframes.Count - 1)
                return Loop ? GetRotationAtTime(timeMs % Keyframes[^1].Time) : currentRot;

            var nextRot = Keyframes[index + 1].Transform.Rotation;
            var result = MathFunctions.Map(
                timeMs,
                Keyframes[index].Time,
                Keyframes[index + 1].Time,
                currentRot,
                nextRot
            );
            return result;
        }

        public DynamicTransform GetTransformAtTime(float timeMs)
        {
            var index = FindKeyframeAfterTime(timeMs);
            if (index == -1)
                return DynamicTransform.Default;

            var transform = Keyframes[index].Transform;
            if (index == Keyframes.Count - 1)
                return Loop ? GetTransformAtTime(timeMs % Keyframes[^1].Time) : transform;

            var nextTransform = Keyframes[index + 1].Transform;

            //return new DynamicTransform(transform.Matrix);

            var pos = MathFunctions.Map(
                timeMs,
                Keyframes[index].Time,
                Keyframes[index + 1].Time,
                transform.Position,
                nextTransform.Position
            );
            var scale = MathFunctions.Map(
                timeMs,
                Keyframes[index].Time,
                Keyframes[index + 1].Time,
                transform.Scale,
                nextTransform.Scale
             );
            var rot = MathFunctions.Map(
                timeMs,
                Keyframes[index].Time,
                Keyframes[index + 1].Time,
                transform.Rotation,
                nextTransform.Rotation
            );

            var percent = (float)MathFunctions.Map(timeMs, Keyframes[index].Time, Keyframes[index + 1].Time, 0, 1);

            Debug.Assert(percent > 0 && percent < 1);

            return new DynamicTransform(MathFunctions.Tween(transform.Matrix, nextTransform.Matrix, percent));
        }

        private int FindKeyframeAfterTime(float timeMs) => Keyframes.FindLastIndex(keyframe => timeMs > keyframe.Time);
    }
}
