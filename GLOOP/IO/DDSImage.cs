using GLOOP.Rendering;
using OpenTK.Graphics.OpenGL4;
using Pfim;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GLOOP.IO
{
    public static class DDSImage
    {
        public static IImage Load(string path, TextureParams settings, bool decompress)
        {
            var image = Pfim.Pfim.FromFile(path, new PfimConfig(decompress: decompress));
            if (decompress || image is UncompressedDds)
            {
                return loadUncompressed((UncompressedDds)image, settings);
            }
            else
            {
                return loadCompressed((CompressedDds)image, settings);
            }
        }

        public static byte[] GetPixelData(string path, out int width, out int height)
        {
            var image = (UncompressedDds)Pfim.Pfim.FromFile(path, new PfimConfig(decompress: true));
            width = image.Width;
            height = image.Height;
            return image.Data;
        }

        private static IImage loadCompressed(CompressedDds image, TextureParams settings)
        {
            var data = Marshal.UnsafeAddrOfPinnedArrayElement(image.Data, 0);
            var internalFormat = image.Header.PixelFormat.FourCC switch
            {
                CompressionAlgorithm.D3DFMT_DXT1 => PixelInternalFormat.CompressedSrgbAlphaS3tcDxt1Ext,
                CompressionAlgorithm.D3DFMT_DXT3 => PixelInternalFormat.CompressedSrgbAlphaS3tcDxt3Ext,
                CompressionAlgorithm.D3DFMT_DXT5 => PixelInternalFormat.CompressedSrgbAlphaS3tcDxt5Ext,
                CompressionAlgorithm.ATI2 => (PixelInternalFormat)OpenTK.Graphics.OpenGL.All.CompressedLuminanceAlphaLatc2Ext, // Not sure
                CompressionAlgorithm.BC5U => (PixelInternalFormat)OpenTK.Graphics.OpenGL.All.CompressedLuminanceAlphaLatc2Ext
            };

            settings.InternalFormat = internalFormat;
            settings.Data = data;
            settings.CompressedDataLength = image.Data.Length;
            return image;
        }

        private static IImage loadUncompressed(UncompressedDds uncompressedDDS, TextureParams settings)
        {
            var internalFormat = uncompressedDDS.Format switch
            {
                ImageFormat.Rgba16 => PixelInternalFormat.Rgba4,
                ImageFormat.Rgb24 => PixelInternalFormat.Rgb8,
                ImageFormat.Rgba32 => PixelInternalFormat.Rgba8
            };
            var pixelFormat = uncompressedDDS.Format switch
            {
                ImageFormat.Rgb24 => PixelFormat.Bgr,
                ImageFormat.Rgba32 => PixelFormat.Bgra
            };

            settings.InternalFormat = internalFormat;
            settings.PixelFormat = pixelFormat;
            settings.Data = Marshal.UnsafeAddrOfPinnedArrayElement(uncompressedDDS.Data, 0);

            return uncompressedDDS;
        }
    }
}
