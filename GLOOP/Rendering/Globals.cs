using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public static class Globals
    {
        private static int? maxLabelLength, uniformBufferOffsetAlignment, maxTextureUnits, maxDebugGroupNameLength;
        private static float? maxTextureAnisotropy;

        public static int MaxLabelLength => maxLabelLength ??= GL.GetInteger((GetPName)All.MaxLabelLength);

        public static int UniformBufferOffsetAlignment => uniformBufferOffsetAlignment ??= GL.GetInteger((GetPName)All.UniformBufferOffsetAlignment);

        public static int MaxTextureUnits => maxTextureUnits ??= GL.GetInteger((GetPName)All.MaxTextureImageUnits);

        public static int MaxDebugGroupNameLength => maxDebugGroupNameLength ??= GL.GetInteger((GetPName)All.MaxDebugMessageLength);

        public static float MaxTextureAnisotropy => maxTextureAnisotropy ??= GL.GetFloat((GetPName)All.MaxTextureMaxAnisotropy);
    }
}
