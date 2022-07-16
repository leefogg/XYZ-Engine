using GLOOP.Util;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GLOOP.Animation
{
    public class Timeline<KeyframeType, OutputType> where KeyframeType : Keyframe<OutputType>
    {
        public bool Loop = true;

        public float LengthMs => Keyframes[^1].TimeMs;

        private readonly List<KeyframeType> Keyframes;

        public Timeline(bool loop)
        {
            Loop = loop;
            Keyframes = new List<KeyframeType>();
        }

        public Timeline(int initialKeyframes = 16)
        {
            Keyframes = new List<KeyframeType>(initialKeyframes);
        }
        public Timeline(Span<KeyframeType> transforms)
            : this(transforms.Length)
        {
            for (int i = 0; i < transforms.Length; i++)
                AddKeyframe(transforms[i]);
        }

        
        public void AddKeyframe(KeyframeType keyframe)
        {
            Keyframes.Add(keyframe);
        }

        public OutputType GetValueAtTime(float timeMs)
        {
            if (Loop)
                timeMs %= LengthMs;
            else if (timeMs >= LengthMs)
                timeMs = LengthMs;

            var index = FindKeyframeAfterTime(timeMs);
            if (index == -1)
                return Keyframes[0].Value;

            var keyframe = Keyframes[index];
            var nextKeyframe = Keyframes[(index + 1) % Keyframes.Count];

            var percent = (float)MathFunctions.Map(timeMs, keyframe.TimeMs, nextKeyframe.TimeMs, 0, 1);

            Debug.Assert(percent >= 0 && percent <= 1, "Percent is out of range.");

            return keyframe.Tween(nextKeyframe, percent);
        }

        private int FindKeyframeAfterTime(float timeMs) => Keyframes.FindLastIndex(keyframe => timeMs >= keyframe.TimeMs);
    }
}
