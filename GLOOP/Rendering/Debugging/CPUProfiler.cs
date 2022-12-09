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
    public static class CPUProfiler
    {
        // Graph Rendering
        private const int FrameWidth = 2;
        private const int FrameSpacing = 0;
        private const int TaskMarkerWidth = 20;

        public enum Event
        {
            Geomertry,
            PortalCulling,
            Lighting,
            Bloom,
            PostEffects,
            Graph,
            ImGUI,
            UpdateBuffers,
            Count
        }
        private static uint[] Colors = new uint[(int)Event.Count]
        {
            0xFF0000FF, // Geometry
            0xFFFF8080, // Portal culling
            0xFFA0A0A0, // Lighting
            0xFF00FF00, // ImGui
            0xFF0050A0, // Post effets
            0xFF505050, // Graph
            0xFF745D81, // Update Buffers
            0xFF123456, // ImGUI
        };

        public class Frame : DummyDisposable
        {
#if !RELEASE
            internal double EndMs;

            internal readonly CPUEventTiming[] EventTimings = new CPUEventTiming[(int)Event.Count];

            public Frame()
            {
                for (int i = 0; i < EventTimings.Length; i++)
                    EventTimings[i] = new CPUEventTiming();
            }

            public override void Dispose() => EndMs = Window.FrameMillisecondsElapsed;

            public IDisposable this[Event index]
            {
                get
                {
                    var e = EventTimings[(int)index];
                    e.Start();
                    return e;
                }
            }

            public IDisposable PeekEvent(Event index) => EventTimings[(int)index];

            internal void Zero()
            {
                foreach (var evnt in EventTimings)
                    evnt.Zero();
            }

#else
            public Frame() { }

            public IDisposable PeekEvent(Event index) => DummyDisposable.Instance;
            internal void Zero() { }
            public IDisposable this[Event index] => DummyDisposable.Instance;
#endif
        }

        public class CPUEventTiming : DummyDisposable
        {
#if !RELEASE
            public float StartMs { get; internal set; }
            public float EndMs { get; internal set; }

            internal void Start() => StartMs = Window.FrameMillisecondsElapsed;

            public override void Dispose() => EndMs = Window.FrameMillisecondsElapsed;

            internal void Zero() => StartMs = EndMs = 0f;

            public override string ToString() => $"{StartMs}-{EndMs}";
#endif
        }

        private static Ring<Frame> TimingsRingbuffer = new Ring<Frame>(
            PowerOfTwo.OneHundrendAndTwentyEight, 
            i => new Frame()
        );

        public static Frame NextFrame
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
        [Conditional("PROFILE")]
        public static void Render(Frame currentFrame)
        {
#if !RELEASE
            using var profiler = EventProfiler.Profile("Render Graph");
            using var timer = currentFrame[Event.Graph];
            if (!ImGui.Begin("CPU Frame Profiler"))
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
            const int maxMs = 10;
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
            var yOffset = 0;
            for (int i = 0; i < (int)Event.Count; i++)
            {
                var evnt = lastFrame.EventTimings[i];
                var startY = evnt.StartMs * scalar;
                var endY = evnt.EndMs * scalar;
                if (startY != endY)
                {

                    var polyVerts = new[]
                    {
                        pos - new Vector2(0, endY),
                        pos - new Vector2(-TaskMarkerWidth, squareHeight + yOffset),
                        pos + new Vector2(TaskMarkerWidth, -yOffset),
                        pos - new Vector2(0, startY),
                    };

                    drawList.AddConvexPolyFilled(ref polyVerts[0], 4, Colors[i]);
                    yOffset += squareHeight;
                }
            }
            pos.X += TaskMarkerWidth;

            var offset = new Vector2(0, 0);
            var rectOffset = new Vector2(10, -squareHeight);
            var textOffset = new Vector2(15, -squareHeight);
            for (int i = 0; i < (int)Event.Count; i++)
            {
                var evnt = lastFrame.EventTimings[i];
                var length = evnt.EndMs - evnt.StartMs;
                if (length > 0)
                {
                    drawList.AddRectFilled(pos - offset, pos + rectOffset - offset, Colors[i]);
                    var text = $"[{evnt.EndMs - evnt.StartMs:00.000}ms] {Enum.GetName(typeof(Event), (Event)i)}";
                    drawList.AddText(pos + textOffset - offset, Colors[i], text);
                offset.Y += 15;
                }
            }

            ImGui.End();
#endif
        }
    }
}
