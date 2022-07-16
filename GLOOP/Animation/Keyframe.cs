using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Animation
{
    public abstract class Keyframe<OutputType>
    {
        public float TimeMs;

        protected Keyframe(float timeMs)
        {
            TimeMs = timeMs;
        }

        public abstract OutputType Value { get; }

        public abstract OutputType Tween(Keyframe<OutputType> other, float percent);
    }
}
