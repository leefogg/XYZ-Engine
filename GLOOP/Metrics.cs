using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public class Metrics
    {
        // Loading stuff
        public static ulong TexturesBytesUsed = 0;
        public static ulong ModelsVertciesBytesUsed = 0;
        public static ulong ModelsIndiciesBytesUsed = 0;
        public static ulong TextureCount = 0;
        public static TimeSpan TimeLoadingTextures, TimeLoadingModels;

        // Per-frame stuff
        public static int ModelsDrawn = 0;
        public static int LightsDrawn = 0;
    }
}
