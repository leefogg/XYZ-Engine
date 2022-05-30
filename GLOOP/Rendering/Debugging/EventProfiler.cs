using GLOOP.Util;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GLOOP.Rendering.Debugging
{
    public static class EventProfiler
    {
        public class Timer : DummyDisposable
        {
            public float StartMs;
            private string FunctionName;
            public Timer(string name)
            {
                FunctionName = name;
            }

            public void Start()
            {
                StartMs = Window.FrameMillisecondsElapsed;
            }

            public override void Dispose()
            {
                var endMs = Window.FrameMillisecondsElapsed;
                EventTimings[FunctionName] += Math.Max(0, endMs - StartMs);
            }
        }

        private static Dictionary<string, float> EventTimings = new Dictionary<string, float>();
        private static Dictionary<string, Timer> Timers = new Dictionary<string, Timer>();

        public static Timer Profile([System.Runtime.CompilerServices.CallerMemberName] string functionName = "")
        {
            Timer timer;
            if (!Timers.TryGetValue(functionName, out timer))
            {
                timer = new Timer(functionName);
                Timers.Add(functionName, timer);
                EventTimings.Add(functionName, 0);
            }

            timer.Start();
            return timer;
        }

        [Conditional("DEBUG")]
        [Conditional("BETA")]
        public static void NewFrame()
        {
            var keys = new string[EventTimings.Keys.Count];
            EventTimings.Keys.CopyTo(keys, 0);
            foreach (var key in keys)
                EventTimings[key] = 0;
        }

        [Conditional("DEBUG")]
        [Conditional("BETA")]
        public static void DrawImGuiWindow()
        {
            if (!ImGui.Begin("Event Profiler"))
                return;

            var drawList = ImGui.GetWindowDrawList();
            var pos = ImGui.GetWindowPos();
            var windowSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
            pos += ImGui.GetWindowContentRegionMin();
            //pos.Y += windowSize.Y;
            //pos.Y -= 20;

            var padding = 6;
            var barHeight = windowSize.Y / EventTimings.Values.Count;

            var labels = EventTimings.Keys.Select(key => (Key: key, WidthPx: ImGui.CalcTextSize(key).X));

            var startPos = pos;

            pos.Y += padding;
            foreach (var (functionName, _) in labels)
            {
                drawList.AddText(pos, 0xFFFFFFFF, functionName);
                pos.Y += barHeight;
            }

            pos = startPos;
            var longestLabel = labels.Max(l => l.WidthPx);
            pos.X += longestLabel + padding;
            foreach (var (functionName, timing) in EventTimings)
            {
                drawList.AddRectFilled(
                    pos,
                    pos + new System.Numerics.Vector2((int)Math.Ceiling(timing * 100f), barHeight - padding),
                    0xFFFFFFFF
                );
                pos.Y += barHeight;
            }

            ImGui.End();
        }
    }
}
