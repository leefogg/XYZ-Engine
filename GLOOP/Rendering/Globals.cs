using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GLOOP.Rendering
{
    public static class Globals
    {
        public class Limits
        {
            public static readonly int MaxBonesPerModel = 1024;
        }

        private static int? maxLabelLength, uniformBufferOffsetAlignment, maxTextureUnits, maxDebugGroupNameLength;
        private static float? maxTextureAnisotropy;
        private static string[]? supportedExtensions;

        public static int MaxLabelLength => maxLabelLength ??= GL.GetInteger((GetPName)All.MaxLabelLength);

        public static int UniformBufferOffsetAlignment => uniformBufferOffsetAlignment ??= GL.GetInteger((GetPName)All.UniformBufferOffsetAlignment);

        public static int MaxTextureUnits => maxTextureUnits ??= GL.GetInteger((GetPName)All.MaxTextureImageUnits);

        public static int MaxDebugGroupNameLength => maxDebugGroupNameLength ??= GL.GetInteger((GetPName)All.MaxDebugMessageLength);

        public static float MaxTextureAnisotropy => maxTextureAnisotropy ??= GL.GetFloat((GetPName)All.MaxTextureMaxAnisotropy);

        public static string[] SupportedExtensions
        {
            get
            {
                if (supportedExtensions == null)
                {
                    var numExtensions = GL.GetInteger(GetPName.NumExtensions);
                    supportedExtensions = new string[numExtensions];
                    for (int i = 0; i < numExtensions; i++)
                        supportedExtensions[i] = GL.GetString(StringNameIndexed.Extensions, i);
                }

                return supportedExtensions;
            }
        }

        public static bool SupportsExtension(string extensionName) => SupportedExtensions.Contains(extensionName);
    }
}
