using GLOOP.Util;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GLOOP.Animation
{
    public class TransformTimeline
    {
        public bool Loop = false;

        internal class Keyframe
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

        private int FindKeyframeAfterTime(float timeMs) => Keyframes.FindLastIndex(keyframe => timeMs > keyframe.Time);
    }
}
