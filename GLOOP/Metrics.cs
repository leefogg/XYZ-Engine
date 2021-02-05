using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public class Metrics
    {
        public static ulong TexturesBytesUsed = 0;
        public static ulong ModelsBytesUsed = 0;
        public static ulong ModelsIndiciesBytesUsed = 0;
        public static ulong TextureCount = 0;
        public static TimeSpan TimeLoadingTextures, TimeLoadingModels;
    }
}
