using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
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
        }
    }
}
