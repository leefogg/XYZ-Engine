using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Animation
{
    public interface IReadonlyTimeline<KeyframeType, OutputType> where KeyframeType : Keyframe<OutputType>
    {
        public IReadOnlyList<KeyframeType> Keyframes { get; }
        public float LengthMs { get; }

        public OutputType GetValueAtTime(float timeMs);
    }
}
