using GLOOP.Extensions;
using GLOOP.Util;
using GLOOP.Util.Structures;
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
        public ref struct Timer 
        {
#if !RELEASE
            private float StartMs;
            internal int FuncIndex;
            public Timer(int index)
            {
                FuncIndex = index;
                StartMs = Window.FrameMillisecondsElapsed;
            }

            public void Dispose()
            {
                var endMs = Window.FrameMillisecondsElapsed;
                if (FuncIndex < EventTimings.Current.Length)
                    EventTimings.Current[FuncIndex] += Math.Max(0, endMs - StartMs);
                else
                    Debug.Fail("Not enough space to store timing for function " + FunctionNames[FuncIndex]);
            }
#else
            public Timer(int index) { }
            public void Dispose() { }
#endif
        }

        private const int MAX_TRACKED_FUNCTIONS = 32;
        private static readonly string[] FunctionNames = new string[MAX_TRACKED_FUNCTIONS];
        private static readonly Ring<float[]> EventTimings = new Ring<float[]>(PowerOfTwo.Two, i => new float[MAX_TRACKED_FUNCTIONS]);
        private static int NumTrackedFunctions;

        public static float GetTiming(string funcName) 
        {
            var index = FunctionNames.IndexOf(funcName);
            if (index == -1)
                return 0;
            return EventTimings.Current[index];
        }

        public static Timer Profile([System.Runtime.CompilerServices.CallerMemberName] string functionName = "")
        {
            var nameIdx = FunctionNames.IndexOf(functionName);
            if (nameIdx == -1)
            {
                nameIdx = NumTrackedFunctions;
                FunctionNames[NumTrackedFunctions] = functionName;
                NumTrackedFunctions++;
            }

            return new Timer(nameIdx);
        }

        [Conditional("DEBUG")]
        [Conditional("BETA")]
        [Conditional("PROFILE")]
        public static void NewFrame()
        {
            EventTimings.MoveNext();
            Array.Clear(EventTimings.Current, 0, NumTrackedFunctions);
        }

        [Conditional("DEBUG")]
        [Conditional("BETA")]
        [Conditional("PROFILE")]
        public static void DrawImGuiWindow()
        {
            if (!ImGui.Begin("Event Profiler"))
                return;

            var drawList = ImGui.GetWindowDrawList();
            var pos = ImGui.GetWindowPos();
            var windowSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
            pos += ImGui.GetWindowContentRegionMin();

            var padding = 6;
            var barHeight = windowSize.Y / NumTrackedFunctions;

            var startPos = pos;

            pos.Y += padding;
            for (int i = 0; i < NumTrackedFunctions; i++)
            {
                drawList.AddText(pos, 0xFFFFFFFF, FunctionNames[i]);
                pos.Y += barHeight;
            }

            pos = startPos;
            var longestLabel = FunctionNames.Max(key => ImGui.CalcTextSize(key).X);
            pos.X += longestLabel + padding;
            for (int i = 0; i < NumTrackedFunctions; i++)
            {
                var functionName = FunctionNames[i];
                var timing = EventTimings.Current[i];
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
