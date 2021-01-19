using System;
using System.Drawing;
using System.Drawing.Imaging;
using GLOOP.Extensions;
using OpenTK.Graphics.OpenGL4;
using static OpenTK.Graphics.OpenGL.GL;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace GLOOP.Rendering
{
    public class Texture : BaseTexture
    {
        private Lazy<ulong> bindlessHandle;
        public ulong BindlessHandle => bindlessHandle.Value;

        public Texture(string path, PixelInternalFormat internalFormat = PixelInternalFormat.Rgba)
            : this(path, new TextureParams() { InternalFormat = internalFormat })
        {}

        public Texture(string path, TextureParams settings)
            : base(TextureTarget.Texture2D)
        {
            Use(); // Might not be needed

            bindlessHandle = new Lazy<ulong>(makeTexureResident);

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

        public Texture(int width, int height, TextureParams settings, string name = null)
            : base(TextureTarget.Texture2D)
        {
            construct(width, height, settings, name);
        }
        
        private void construct(int width, int height, TextureParams settings, string name = null)
        {
            Use();

            if (!string.IsNullOrEmpty(name))
            {
                name = name[..Math.Min(name.Length, Globals.MaxLabelLength)];
                GL.ObjectLabel(ObjectLabelIdentifier.Texture, Handle, name.Length, name);
            }

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

            configureParams(settings.WrapMode, settings.MinFilter, settings.MagFilter);

            if (settings.GenerateMips)
                GenerateMips();

            ResourceManager.Add(this);

            GPUResource.TexturesBytesUsed += (ulong)(settings.InternalFormat.GetSizeInBytes() * width * height);
            GPUResource.TextureCount++;
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

        private ulong makeTexureResident()
        {
            Use();

            var handle = (ulong)Arb.GetTextureHandle(Handle);
            Arb.MakeTextureHandleResident(handle);
            return handle;
        }
    }
}
