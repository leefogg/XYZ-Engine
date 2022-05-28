using GLOOP.Util;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            pos.Y += windowSize.Y;
            pos.Y -= 20;

            var padding = 6;
            var barWidth = windowSize.X / EventTimings.Values.Count;
            //barWidth -= padding * (EventTimings.Values.Count-1);
            //drawList.AddRectFilled(pos, pos + new System.Numerics.Vector2(barWidth, -100), 0xFFFFFFFF);

            foreach (var (functionName, timing) in EventTimings)
            {
                drawList.AddRectFilled(pos, pos + new System.Numerics.Vector2(barWidth - padding, (int)Math.Ceiling(-timing * 100f)), 0xFFFFFFFF);
                drawList.AddText(pos + new System.Numerics.Vector2(0, 10), 0xFFFFFFFF, functionName);
                pos.X += barWidth;
            }

            ImGui.End();
        }
    }
}
