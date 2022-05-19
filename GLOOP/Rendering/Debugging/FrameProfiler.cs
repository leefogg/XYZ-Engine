using GLOOP.Util;
using GLOOP.Util.Structures;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;

namespace GLOOP.Rendering.Debugging
{
    public static class FrameProfiler
    {
        private const int FrameWidth = 2;
        private const int FrameSpacing = 0;
        private const int taskMarkerWidth = 20;

        public enum Event
        {
            UpdateBuffers,
            Geomertry,
            PortalCulling,
            Lighting,
            Bloom,
            PostEffects,
            Graph,
            ImGui,
            Count
        }
        private static uint[] Colors = new uint[(int)Event.Count]
        {
            0xFF74DD81,
            0xFF0000FF, // Geo
            0xFFFF8080, // Portal culling
            0xFFA0A0A0, // Lighting
            0xFF00FF00, // ImGui
            0xFF0050A0, // Post effets
            0xFF505050, // Graph
            0xFF808080
        };

        public class Frame : DummyDisposable
        {
#if !RELEASE
            internal double EndMs;

            internal readonly EventTiming[] EventTimings = new EventTiming[(int)Event.Count];

            public Frame()
            {
                for (int i = 0; i < EventTimings.Length; i++)
                    EventTimings[i] = new EventTiming();
            }

            public override void Dispose() => EndMs = Window.FrameMillisecondsElapsed;
#endif
            public IDisposable this[Event index]
            {
                get
                {
#if !RELEASE
                    var e = EventTimings[(int)index];
                    e.StartMs = Window.FrameMillisecondsElapsed;
                    return e;
#else
                    return DummyDisposable.Instance;
#endif
                }
            }


            internal void Zero()
            {
#if !RELEASE
                foreach (var evnt in EventTimings)
                    evnt.Zero();
#endif
            }

        }

        public class EventTiming : DummyDisposable
        {
#if !RELEASE
            internal float StartMs, EndMs;

            public override void Dispose()
            {
                EndMs = Window.FrameMillisecondsElapsed;
            }

            internal void Zero()
            {
                StartMs = EndMs = 0f;
            }

            public override string ToString() => $"{StartMs}-{EndMs}";
#endif
        }

        private static Ring<Frame> TimingsRingbuffer = new Ring<Frame>(
            PowerOfTwo.OneHundrendAndTwentyEight, 
            i => new Frame()
        );

        public static Frame CurrentFrame
        {
            get
            {
                var next = TimingsRingbuffer.Next;
                next.Zero();
                return next;
            }
        }

        [Conditional("DEBUG")]
        [Conditional("BETA")]
        public static void Render(Frame currentFrame)
        {
#if !RELEASE
            using var timer = currentFrame[Event.Graph];
            if (!ImGui.Begin("Profiler"))
                return;

            var drawList = ImGui.GetWindowDrawList();
            var pos = ImGui.GetWindowPos();
            var windowSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
            pos += ImGui.GetWindowContentRegionMin();
            drawList.AddRect(pos, pos + new Vector2(TimingsRingbuffer.Count * (FrameWidth + FrameSpacing), windowSize.Y), 0xFFFFFFFF, 0);
            pos.Y += windowSize.Y;
            pos.X += 1;
            pos.Y += 1;
            var frameSize = new Vector2(FrameWidth, 0);
            var start = new Vector2(0, 0);
            var end = new Vector2(0, 0);
            const int maxMs = 4;
            float scalar = windowSize.Y / maxMs;
            Frame lastFrame = null;
            foreach (var frame in TimingsRingbuffer)
            {
                var frameLength = frame.EventTimings[(int)Event.Count - 1].EndMs;
                start.Y = 0;
                end.Y = frameLength * scalar;
                drawList.AddRectFilled(pos - start, pos - end + frameSize, 0xFFFFFFFF, 0);

                for (int i = 0; i < (int)Event.Count; i++)
                {
                    var evnt = frame.EventTimings[i];
                    start.Y = evnt.StartMs * scalar;
                    end.Y = evnt.EndMs * scalar;
                    drawList.AddRectFilled(pos - start, pos - end + frameSize, Colors[i], 0);
                }

                pos.X += FrameWidth + FrameSpacing;
                lastFrame = frame;
            }

            pos.X += FrameSpacing;

            const int squareHeight = 15;
            for (int i = 0; i < (int)Event.Count; i++)
            {
                var yOffset = squareHeight * i;
                var evnt = lastFrame.EventTimings[i];
                var startY = evnt.StartMs * scalar;
                var endY = evnt.EndMs * scalar;
                var polyVerts = new[]
                {
                    pos - new Vector2(0, endY),
                    pos - new Vector2(-taskMarkerWidth, squareHeight + yOffset),
                    pos + new Vector2(taskMarkerWidth, -yOffset),
                    pos - new Vector2(0, startY),
                };

                drawList.AddConvexPolyFilled(ref polyVerts[0], 4, Colors[i]);
            }
            pos.X += taskMarkerWidth;

            var offset = new Vector2(0, 0);
            var rectOffset = new Vector2(10, -squareHeight);
            var textOffset = new Vector2(15, -squareHeight);
            for (int i = 0; i < (int)Event.Count; i++)
            {
                var evnt = lastFrame.EventTimings[i];
                drawList.AddRectFilled(pos - offset, pos + rectOffset - offset, Colors[i]);
                var text = $"[{evnt.EndMs - evnt.StartMs:0.00}ms] {Enum.GetName(typeof(Event), (Event)i)}";
                drawList.AddText(pos + textOffset - offset, Colors[i], text);
                offset.Y += 15;
            }

            ImGui.End();
#endif
        }
    }
}
