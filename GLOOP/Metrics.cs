using System;
using System.Collections.Generic;
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

        public static void ResetFrameCounters()
        {
            RecordingStream?.WriteLine(
                string.Format(
                    "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}",
                    Window.FrameNumber,
                    Window.FrameMillisecondsElapsed,
                    ModelsDrawn,
                    LightsDrawn,
                    RenderBatches,
                    QueriesPerformed,
                    ShaderBinds,
                    TextureSetBinds,
                    BufferBinds,
                    FrameBufferBinds,
                    BufferReads,
                    BufferWrites
                )
            );

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

        public static void StartRecording(string filename)
        {
            RecordingStream = File.CreateText(filename);
            RecordingStream.WriteLine(
                "Frame,Frame Milliseconds,Models Drawn,Lights Drawn,Render Batches,Queries Performed,Shader Binds,Texture Set Binds,Buffer Binds,FrameBuffer Binds,Buffer Reads,Buffer Writes"
            );
        }

        public static void StopRecording()
        {
            RecordingStream.Flush();
            RecordingStream.Close();
            RecordingStream.Dispose();
            RecordingStream = null;
        }
    }
}
