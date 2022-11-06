using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GLOOP.Rendering
{
    public static class Globals
    {
        public static class Limits
        {
            public static readonly int MaxBonesPerModel = 1024;
        }

        public static class BindingPoints
        {
            // Probably want to move some of these out to a rendering technique class when we have it
            // TODO: Would be good to have a typedef so cant accidentally use wrong type
            public static class UBOs
            {
                public static readonly int Camera = 0;
                public static readonly int SkeletonBonePoses = 2;
                public static readonly int Bloom = 3;

                public static class DeferredRendering
                {
                    public static readonly int PointLights = 1;
                    public static readonly int SpotLights = 1;
                }
            }
            public static class SSBOs
            {
                public static class DeferredRendering
                {
                    public static readonly int Models = 1;
                    public static readonly int PointLights = 1;
                    public static readonly int SpotLights = 1;
                }
            }
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
