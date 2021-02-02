using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using GLOOP.Extensions;
using OpenTK.Graphics.OpenGL4;
using Pfim;
using static OpenTK.Graphics.OpenGL.GL;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace GLOOP.Rendering
{
    public class Texture : BaseTexture
    {
        public Texture(string path, PixelInternalFormat internalFormat = PixelInternalFormat.Rgba)
            : this(path, new TextureParams() { InternalFormat = internalFormat })
        {}

        public Texture(string path, TextureParams settings)
            : base(TextureTarget.Texture2D)
        {
            Use(); // Might not be needed

            if (string.IsNullOrEmpty(settings.Name))
                settings.Name = Path.GetFileName(path);

            if (Path.GetExtension(path).ToLower().EndsWith("dds"))
            {
                using var image = (CompressedDds)Pfim.Pfim.FromFile(path, new PfimConfig(decompress: false));
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

                
                construct(image.Width, image.Height, settings);
            }
            else
            {
                using var image = new Bitmap(path);
                var data = image.LockBits(
                    new Rectangle(0, 0, image.Width, image.Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb
                );

                settings.PixelFormat = PixelFormat.Bgra;
                settings.Data = data.Scan0;
                construct(image.Width, image.Height, settings);
            }
        }

        public Texture(int width, int height, TextureParams settings)
            : base(TextureTarget.Texture2D)
        {
            construct(width, height, settings);
        }
        
        private void construct(int width, int height, TextureParams settings)
        {
            Use();

            var name = settings.Name;
            if (!string.IsNullOrEmpty(name))
            {
                name = name[..Math.Min(name.Length, Globals.MaxLabelLength)];
                GL.ObjectLabel(ObjectLabelIdentifier.Texture, Handle, name.Length, name);
            }

            int numPixels = 0;
            if (settings.CompressedDataLength > 0)
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
                var compressedBytesPerBlock = settings.InternalFormat switch
                {
                    PixelInternalFormat.CompressedSrgbAlphaS3tcDxt1Ext => 8,
                    PixelInternalFormat.CompressedSrgbAlphaS3tcDxt3Ext => 16,
                    PixelInternalFormat.CompressedSrgbAlphaS3tcDxt5Ext => 16,
                    (PixelInternalFormat)OpenTK.Graphics.OpenGL.All.CompressedLuminanceAlphaLatc2Ext => 16
                };
                var offset = 0;
                var i = 0;
                while (offset < settings.CompressedDataLength)
                {
                    var size = Math.Max(1, ((width + 3) / 4)) * Math.Max(1, ((height + 3) / 4)) * compressedBytesPerBlock;
                    GL.CompressedTexImage2D(
                        TextureTarget.Texture2D,
                        i,
                        (InternalFormat)settings.InternalFormat,
                        width,
                        height,
                        0,
                        size,
                        settings.Data + offset
                    );

                    numPixels += width * height;

                    offset += size;
                    i++;
                    width = Math.Max(1, width / 2);
                    height = Math.Max(1, height / 2);
                }
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, i);
            }
            else
            {
                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    settings.InternalFormat,
                    width,
                    height,
                    0,
                    settings.PixelFormat,
                    PixelType.UnsignedByte,
                    settings.Data
                );
                numPixels = width * height;
            }

            configureParams(settings.WrapMode, settings.MinFilter, settings.MagFilter);

            if (settings.GenerateMips)
                GenerateMips();

            ResourceManager.Add(this);

            Metrics.TexturesBytesUsed += (ulong)(settings.InternalFormat.GetSizeInBytes() * numPixels);
            Metrics.TextureCount++;
        }

        private void GenerateMips()
        {
            Use();

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        private static void configureParams(TextureWrapMode repeatMode, TextureMinFilter minFilter, TextureMinFilter magFilter)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)repeatMode);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)repeatMode);
        }


    }
}
