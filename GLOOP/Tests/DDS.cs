using GLOOP.Rendering;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using Pfim;

namespace GLOOP.Tests
{
    public class DDS : Window
    {
        public DDS(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            //var path = @"C:\dds\tau_modules_xx.dds";
            var path = @"C:\dds\elevator_enginehouse_nrm.dds";
            using var image = (CompressedDds)Pfim.Pfim.FromFile(path, new PfimConfig(decompress: false));

            var tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            var handle = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
            var offset = 0;
            var width = image.Width;
            var height = image.Height;
            var i = 0;
            var internalFormat = image.Header.PixelFormat.FourCC switch
            {
                CompressionAlgorithm.D3DFMT_DXT1 => InternalFormat.CompressedRgbaS3tcDxt1Ext,
                CompressionAlgorithm.D3DFMT_DXT3 => InternalFormat.CompressedRgbaS3tcDxt3Ext,
                CompressionAlgorithm.D3DFMT_DXT5 => InternalFormat.CompressedRgbaS3tcDxt5Ext,
                CompressionAlgorithm.ATI2 => (InternalFormat)OpenTK.Graphics.OpenGL.All.CompressedLuminanceAlphaLatc2Ext, // Not sure
                CompressionAlgorithm.BC5U => (InternalFormat)OpenTK.Graphics.OpenGL.All.CompressedLuminanceAlphaLatc2Ext
            };
            var compressedBytesPerBlock = image.Header.PixelFormat.FourCC switch
            {
                CompressionAlgorithm.D3DFMT_DXT1 => 8,
                CompressionAlgorithm.D3DFMT_DXT3 => 16,
                CompressionAlgorithm.D3DFMT_DXT5 => 16,
                CompressionAlgorithm.ATI2 => 16,
                CompressionAlgorithm.BC5U => 16
            };
            var data = Marshal.UnsafeAddrOfPinnedArrayElement(image.Data, 0);
            while (image.Data.Length  - offset > 0)
            {
                var size = Math.Max(1, ((width + 3) / 4)) * Math.Max(1, ((height + 3) / 4)) * compressedBytesPerBlock;
                GL.CompressedTexImage2D(
                    TextureTarget.Texture2D,
                    i,
                    internalFormat,
                    width,
                    height,
                    0,
                    size,
                    data + offset
                );

                offset += size;
                i++;
                width /= 2;
                height /= 2;
                width = Math.Max(1, width);
                height = Math.Max(1, height);
            }
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, i);
        }
    }
}
