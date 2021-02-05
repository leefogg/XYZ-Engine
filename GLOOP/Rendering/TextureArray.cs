using GLOOP.Extensions;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static OpenTK.Graphics.OpenGL.GL;

namespace GLOOP.Rendering
{
    public class TextureArray : BaseTexture
    {
        private TextureShape shape;

        public TextureArray(TextureShape shape, ushort numLayers, string name = "")
            : base(TextureTarget.Texture2DArray)
        {
            this.shape = shape;

            Use();

            if (!string.IsNullOrEmpty(name))
            {
                name = name[..Math.Min(name.Length, Globals.MaxLabelLength)];
                GL.ObjectLabel(ObjectLabelIdentifier.Texture, Handle, name.Length, name);
            }

            GL.TexStorage3D(
                TextureTarget3d.Texture2DArray,
                1,
                shape.Layout.ToSizedFormat(),
                shape.Width,
                shape.Height,
                numLayers
            );

            configureParams(shape.WrapMode, shape.FilterMode);

            if (shape.HasMipMaps)
                GenerateMips();


            Metrics.TexturesBytesUsed += (ulong)(shape.Layout.GetSizeInBytes() * shape.Width * shape.Height * numLayers);
            Metrics.TextureCount++;
        }

        private void GenerateMips()
        {
            Use();

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);
        }

        public TextureSlice WriteSubData(ushort layer, Vector4[] colours)
        {
            Use();

            GL.TexSubImage3D(
                TextureTarget.Texture2DArray,
                0,
                0, 0, layer,
                shape.Width, shape.Height, 1,
                PixelFormat.Rgba,
                PixelType.Float,
                colours.AsFloats()
            );

            return new TextureSlice(this, layer);
        }
        public TextureSlice WriteSubData(ushort layer, IntPtr colours, PixelFormat format = PixelFormat.Rgba)
        {
            Use();

            GL.TexSubImage3D(
                TextureTarget.Texture2DArray,
                0,
                0, 0, layer,
                shape.Width, shape.Height, 1,
                format,
                PixelType.UnsignedByte,
                colours
            );

            return new TextureSlice(this, layer);
        }

        private static void configureParams(TextureWrapMode repeatMode, TextureMinFilter filterMode)
        {
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)filterMode);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)filterMode);

            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)repeatMode);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)repeatMode);
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
