using GLOOP.Util;
using ImGuiNET;
using System.Linq;
using System.Numerics;

namespace GLOOP.Rendering.Debugging
{
    public static class ImGuiWidgets
    {
        public static void AddTimeline(Vector2 chartPos, float[] samples, float timeMs, float startMs, float endMs, float lengthMs, Vector2 chartSize, int numIndicators = 10)
        {
            var numSamples = samples.Length;
            var dl = ImGui.GetWindowDrawList();
            var lineStepWidth = (float)chartSize.X / (float)numSamples;
            //lineStepWidth = Math.Min(1f, Math.Max(0, lineStepWidth));
            var min = samples.Min();
            var max = samples.Max();
            dl.AddRect(chartPos, chartPos + chartSize, 0xFFFFFFFF); // Border

            // Graph
            for (int i = 0; i < numSamples - 1; i++)
            {
                var start = chartPos + new Vector2(i * lineStepWidth, (float)MathFunctions.Map(samples[i + 0], min, max, chartSize.Y, 0));
                var end = chartPos + new Vector2((i + 1) * lineStepWidth, (float)MathFunctions.Map(samples[i + 1], min, max, chartSize.Y, 0));

                var sampleStartMs = (float)MathFunctions.Map(i, 0, numSamples, 0, lengthMs);
                var isInRange = sampleStartMs >= startMs && sampleStartMs <= endMs;
                dl.AddLine(start, end, isInRange ? 0xFFFFFFFF : 0x50FFFFFF);
            }

            // Draw current/start/end position
            var percent = (float)MathFunctions.Map(timeMs, 0, lengthMs, 0, 1);
            dl.AddLine(
                chartPos + new Vector2(percent * chartSize.X, 0),
                chartPos + new Vector2(percent * chartSize.X, chartSize.Y),
                0xFFFFFFFF
            );
            percent = (float)MathFunctions.Map(startMs, 0, lengthMs, 0, 1);
            dl.AddLine(
                chartPos + new Vector2(percent * chartSize.X, 0),
                chartPos + new Vector2(percent * chartSize.X, chartSize.Y),
                0xFFFFFFFF
            );
            percent = (float)MathFunctions.Map(endMs, 0, lengthMs, 0, 1);
            dl.AddLine(
                chartPos + new Vector2(percent * chartSize.X, 0),
                chartPos + new Vector2(percent * chartSize.X, chartSize.Y),
                0xFFFFFFFF
            );

            // X axis notches
            var indicatorStep = (float)numSamples / (float)numIndicators;
            for (float i = 0; i < numSamples; i += indicatorStep)
            {
                var x = (float)MathFunctions.Map(i, 0, numSamples, 0, chartSize.X);
                dl.AddLine(
                    chartPos + new Vector2(x, chartSize.Y + 10),
                    chartPos + new Vector2(x, chartSize.Y - 10),
                    0xFFFFFFFF
                );
            }

            // X axis lables
            for (float i = 0; i <= numSamples; i += indicatorStep)
            {
                var timeAtSampleMs = (float)MathFunctions.Map(i, 0, numSamples, 0, lengthMs);
                var sampleText = timeAtSampleMs.ToString("0.0");
                var textSize = ImGui.CalcTextSize(sampleText);
                var x = (float)MathFunctions.Map(i, 0, numSamples, 0, chartSize.X);
                dl.AddText(chartPos + new Vector2(x, chartSize.Y + textSize.Y), 0xFFFFFFFF, sampleText);
            }

            // Tooltip


        }
    }
}
