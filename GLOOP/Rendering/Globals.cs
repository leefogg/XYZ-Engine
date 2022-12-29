using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace GLOOP.Rendering
{
    public static class Globals
    {
        public static class Limits
        {
        }

        public static class BindingPoints
        {
            // Probably want to move some of these out to a rendering technique class when we have it
            // TODO: Would be good to have a typedef so cant accidentally use wrong type
            public static class UBOs
            {
                public const int Camera = 0;
                public const int Bloom = 3;

                public static class DeferredRendering
                {
                    public const int PointLights = 1;
                    public const int SpotLights = 1;
                }
            }
            public static class SSBOs
            {
                public static class DeferredRendering
                {
                    public const int Models = 1;
                    public const int PointLights = 1;
                    public const int SpotLights = 1;
                    public const int SkeletonBonePoses = 2;
                }
            }
        }

        private static int? maxLabelLength,
            uniformBufferOffsetAlignment, 
            maxTextureUnits, 
            maxDebugGroupNameLength,
            majorVersion,
            minorVersion;
        private static float? maxTextureAnisotropy;
        private static string vendorName, driverVersion, virtualDeviceName;
        private static string[] supportedExtensions;

        public static int MaxLabelLength => maxLabelLength ??= GL.GetInteger((GetPName)All.MaxLabelLength);
        public static int UniformBufferOffsetAlignment => uniformBufferOffsetAlignment ??= GL.GetInteger((GetPName)All.UniformBufferOffsetAlignment);
        public static int MaxTextureUnits => maxTextureUnits ??= GL.GetInteger((GetPName)All.MaxTextureImageUnits);
        public static int MaxDebugGroupNameLength => maxDebugGroupNameLength ??= GL.GetInteger((GetPName)All.MaxDebugMessageLength);
        public static float MaxTextureAnisotropy => maxTextureAnisotropy ??= GL.GetFloat((GetPName)All.MaxTextureMaxAnisotropy);
        public static float MajorVersion => majorVersion ??= GL.GetInteger((GetPName)All.MajorVersion);
        public static float MinorVersion => minorVersion ??= GL.GetInteger((GetPName)All.MinorVersion);
        public static string VendorName => vendorName ??= GL.GetString(StringName.Vendor);
        public static string DriverVersion => driverVersion ??= GL.GetString(StringName.Version);
        public static string VirtualDeviceName => virtualDeviceName ??= GL.GetString(StringName.Renderer);

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

        public static void PrintHardwareInfo()
        {
            Console.WriteLine($"{VirtualDeviceName} version {DriverVersion} by {VendorName}");
            Console.WriteLine($"GL Version {MajorVersion}.{MinorVersion}");
            Console.WriteLine("Supported Extensions:");
            foreach (var extension in SupportedExtensions)
                Console.WriteLine(extension);
        }
    }
}
