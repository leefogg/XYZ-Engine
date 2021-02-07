using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public static class Globals
    {
        private static int? maxLabelLength, uniformBufferOffsetAlignment, maxTextureUnits;

        public static int MaxLabelLength => maxLabelLength ??= GL.GetInteger((GetPName)All.MaxLabelLength);

        public static int UniformBufferOffsetAlignment => uniformBufferOffsetAlignment ??= GL.GetInteger((GetPName)All.UniformBufferOffsetAlignment);

        public static int MaxTextureUnits => maxTextureUnits ??= GL.GetInteger((GetPName)All.MaxTextureImageUnits);
    }
}
