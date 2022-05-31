﻿using GLOOP.Util;
using GLOOP.Util.Structures;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace GLOOP.Rendering.Debugging
{
    public static class GPUProfiler
    {
        // Graph Rendering
        private const int FrameWidth = 2;
        private const int FrameSpacing = 0;
        private const int TaskMarkerWidth = 20;

        public enum Event
        {
            UpdateBuffers,
            Geometry,
            Lighting,
            Post,
            ImGUI,
            Count
        }
        private static uint[] Colors = new uint[(int)Event.Count]
        {
            0xFF745D81, // Update Buffers
            0xFF0000FF, // Geometry
            0xFFA0A0A0, // Lighting
            0xFF00FF00, // Post
            0xFFFF0000  // ImGUI
        };

        public class GPUEventTiming : DummyDisposable
        {
#if !RELEASE
            internal long StartNs, EndNs;

            internal void Start() => StartNs = GetTimestamp();

            public override void Dispose() => EndNs = GetTimestamp();

            internal void Zero() => StartNs = EndNs = 0;

            public override string ToString() => $"{StartNs}-{EndNs}";
#endif
        }

        public class Frame : GPUEventTiming
        {
            internal readonly GPUEventTiming[] EventTimings = new GPUEventTiming[(int)Event.Count];

            public Frame()
            {
#if !RELEASE
                for (int i = 0; i < EventTimings.Length; i++)
                    EventTimings[i] = new GPUEventTiming();
#endif
            }

            public IDisposable this[Event index]
            {
                get
                {
#if !RELEASE
                    var e = EventTimings[(int)index];
                    e.Start();
                    return e;
#else
                    return DummyDisposable.Instance;
#endif
                }
            }

            public IDisposable PeekEvent(Event index) => EventTimings[(int)index];
        }

        private static Ring<Frame> TimingsRingbuffer = new Ring<Frame>(
            PowerOfTwo.OneHundrendAndTwentyEight,
            i => new Frame()
        );

        public static Frame NextFrame
        {
            get
            {
                var frame = TimingsRingbuffer.Next;
#if !RELEASE
                frame.Zero();
                frame.Start();
#endif
                return frame;
            }
        }

        [Conditional("DEBUG")]
        [Conditional("BETA")]
        public static void Render()
        {
#if !RELEASE
            using var profiler = EventProfiler.Profile("Render Graph");
            if (!ImGui.Begin("GPU Frame Profiler"))
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
            const long maxNs = 10_000_000;
            float scalar = windowSize.Y / maxNs;
            Frame lastFrame = null;
            foreach (var frame in TimingsRingbuffer)
            {
                var frameStart = frame.StartNs;
                var frameLength = frame.EndNs - frameStart;
                start.Y = 0;
                end.Y = frameLength * scalar;
                drawList.AddRectFilled(pos - start, pos - end + frameSize, 0xFFFFFFFF, 0);

                for (int i = 0; i < (int)Event.Count; i++)
                {
                    var evnt = frame.EventTimings[i];
                    start.Y = (evnt.StartNs - frameStart) * scalar;
                    end.Y = (evnt.EndNs - frameStart) * scalar;
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
                var startY = (evnt.StartNs - lastFrame.StartNs) * scalar;
                var endY = (evnt.EndNs - lastFrame.StartNs) * scalar;
                var polyVerts = new[]
                {
                    pos - new Vector2(0, endY),
                    pos - new Vector2(-TaskMarkerWidth, squareHeight + yOffset),
                    pos + new Vector2(TaskMarkerWidth, -yOffset),
                    pos - new Vector2(0, startY),
                };

                drawList.AddConvexPolyFilled(ref polyVerts[0], 4, Colors[i]);
            }
            pos.X += TaskMarkerWidth;

            var offset = new Vector2(0, 0);
            var rectOffset = new Vector2(10, -squareHeight);
            var textOffset = new Vector2(15, -squareHeight);
            for (int i = 0; i < (int)Event.Count; i++)
            {
                var evnt = lastFrame.EventTimings[i];
                drawList.AddRectFilled(pos - offset, pos + rectOffset - offset, Colors[i]);
                var text = $"[{((evnt.EndNs - evnt.StartNs)/1000f):0000.000}ms] {Enum.GetName(typeof(Event), (Event)i)}";
                drawList.AddText(pos + textOffset - offset, Colors[i], text);
                offset.Y += 15;
            }

            ImGui.End();
#endif
        }

        private static long GetTimestamp()
        {
            GL.GetInteger64(GetPName.Timestamp, out long now);
            return now;
        }
    }
}