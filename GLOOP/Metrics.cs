using GLOOP.Rendering.Debugging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace GLOOP
{
    //TODO: Make this compile out
    public static class Metrics
    {
        // Loading stuff
        public static ulong TexturesBytesUsed = 0;
        public static ulong ModelsVertciesBytesUsed = 0;
        public static ulong ModelsIndiciesBytesUsed = 0;
        public static ulong TextureCount = 0;
        public static TimeSpan TimeLoadingTextures, TimeLoadingModels;

        // Per-frame stuff
        public static int
            ModelsDrawn,
            LightsDrawn,
            RenderBatches,
            QueriesPerformed,
            ShaderBinds,
            TextureSetBinds,
            BufferBinds,
            FrameBufferBinds;
        public static ulong BufferReads, BufferWrites;

        private static StreamWriter RecordingStream;

        [Conditional("DEBUG")]
        [Conditional("BETA")]
        public static void AssertMetricRanges()
        {
            Debug.Assert(RenderBatches < 180, "Concerning number of render batches");
            Debug.Assert(BufferBinds < 12, "Cercerning number of buffer binds");
            Debug.Assert(ShaderBinds < 150, "Cercerning number of shader binds");
        }

        [Conditional("DEBUG")]
        [Conditional("BETA")]
        public static void ResetFrameCounters()
        {
            ModelsDrawn = 0;
            LightsDrawn = 0;
            RenderBatches = 0;
            QueriesPerformed = 0;
            ShaderBinds = 0;
            TextureSetBinds = 0;
            BufferBinds = 0;
            FrameBufferBinds = 0;
            BufferReads = 0;
            BufferWrites = 0;
        }

        public static void WriteLog(CPUProfiler.Frame cpuFrame, GPUProfiler.Frame gpuFrame)
        {
#if !RELEASE
            float CPUEventLength(CPUProfiler.Event index)
            {
                var e = (CPUProfiler.CPUEventTiming)cpuFrame.PeekEvent(index);
                return (e.EndMs - e.StartMs) * 1000; // To Nanoseconds
            }
            long GPUEventLength(GPUProfiler.Event index)
            {
                var e = (GPUProfiler.GPUEventTiming)gpuFrame.PeekEvent(index);
                return e.EndNs - e.StartNs;
            }

            RecordingStream?.WriteLine(
                "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23}",
                Window.FrameNumber,
                cpuFrame.EndMs * 1_000_000,
                gpuFrame.EndNs - gpuFrame.StartNs,
                ModelsDrawn,
                LightsDrawn,
                RenderBatches,
                QueriesPerformed,
                ShaderBinds,
                TextureSetBinds,
                BufferBinds,
                FrameBufferBinds,
                BufferReads,
                BufferWrites,
                CPUEventLength(CPUProfiler.Event.Visbility),
                CPUEventLength(CPUProfiler.Event.UpdateBuffers),
                CPUEventLength(CPUProfiler.Event.Geomertry),
                CPUEventLength(CPUProfiler.Event.PortalCulling),
                CPUEventLength(CPUProfiler.Event.Lighting),
                CPUEventLength(CPUProfiler.Event.Bloom),
                CPUEventLength(CPUProfiler.Event.PostEffects),
                GPUEventLength(GPUProfiler.Event.UpdateBuffers),
                GPUEventLength(GPUProfiler.Event.Geomertry),
                GPUEventLength(GPUProfiler.Event.Lighting),
                GPUEventLength(GPUProfiler.Event.Post)
            );
#endif
        }

        [Conditional("DEBUG")]
        [Conditional("BETA")]
        public static void StartRecording(string filename)
        {
            RecordingStream = File.CreateText(filename);
            RecordingStream.WriteLine(
                string.Join(',', new[] {
                    "Frame #",
                    "CPU Nanoseconds",
                    "GPU Nanoseconds",
                    "Models Drawn",
                    "Lights Drawn",
                    "Render Batches",
                    "Queries Performed",
                    "Shader Binds",
                    "Texture Set Binds",
                    "Buffer Binds",
                    "FrameBuffer Binds",
                    "Buffer Reads",
                    "Buffer Writes",
                    "CPU Visibility",
                    "CPU Update Buffers",
                    "CPU Geometry",
                    "CPU Portal Culling",
                    "CPU Lighting",
                    "CPU Bloom",
                    "CPU Post Effects",
                    "GPU Update Buffers",
                    "GPU Geometry",
                    "GPU Lighting",
                    "GPU Post",
                })
            );
        }

        [Conditional("DEBUG")]
        [Conditional("BETA")]
        public static void StopRecording()
        {
            if (RecordingStream != null)
            {
                RecordingStream.Flush();
                RecordingStream.Close();
                RecordingStream.Dispose();
                RecordingStream = null;
            }
        }
    }
}
