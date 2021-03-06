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
        public static IImage Load(string path, TextureParams settings)
        {
            var image = (CompressedDds)Pfim.Pfim.FromFile(path, new PfimConfig(decompress: false));
            var data = Marshal.UnsafeAddrOfPinnedArrayElement(image.Data, 0);
            settings.Data = data;
            settings.CompressedDataLength = image.Data.Length;
            settings.InternalFormat = image.Header.PixelFormat.FourCC switch
            {
                CompressionAlgorithm.D3DFMT_DXT1 => PixelInternalFormat.CompressedSrgbAlphaS3tcDxt1Ext,
                CompressionAlgorithm.D3DFMT_DXT3 => PixelInternalFormat.CompressedSrgbAlphaS3tcDxt3Ext,
                CompressionAlgorithm.D3DFMT_DXT5 => PixelInternalFormat.CompressedSrgbAlphaS3tcDxt5Ext,
                CompressionAlgorithm.ATI2 => (PixelInternalFormat)OpenTK.Graphics.OpenGL.All.CompressedLuminanceAlphaLatc2Ext, // Not sure
                CompressionAlgorithm.BC5U => (PixelInternalFormat)OpenTK.Graphics.OpenGL.All.CompressedLuminanceAlphaLatc2Ext
            };

            return image;
        }
    }
}
