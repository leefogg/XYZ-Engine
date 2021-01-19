using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class TextureShape
    {
        public readonly ushort Width, Height;
        public readonly bool HasMipMaps;
        public readonly PixelInternalFormat Layout;
        public readonly TextureWrapMode WrapMode;
        public readonly TextureMinFilter FilterMode;

        public TextureShape(
            ushort width,
            ushort height,
            bool hasMipMaps,
            PixelInternalFormat layout,
            TextureWrapMode wrapMode,
            TextureMinFilter filterMode)
        {
            Width = width;
            Height = height;
            HasMipMaps = hasMipMaps;
            Layout = layout;
            WrapMode = wrapMode;
            FilterMode = filterMode;
        }

        public static bool operator ==(TextureShape left, TextureShape right)
        {
            return left.Width == right.Width
                && left.Height == right.Height
                && left.HasMipMaps == right.HasMipMaps
                && left.Layout == right.Layout
                && left.WrapMode == right.WrapMode
                && left.FilterMode == right.FilterMode;
        }

        public static bool operator !=(TextureShape left, TextureShape right)
        {
            return left.Width != right.Width
                || left.Height != right.Height
                || left.HasMipMaps != right.HasMipMaps
                || left.Layout != right.Layout
                || left.WrapMode != right.WrapMode
                || left.FilterMode != right.FilterMode;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(100);
            sb.Append('{');

            sb.Append("Width: ");
            sb.Append(Width);
            sb.Append(", Height: ");
            sb.Append(Height);
            sb.Append(", HasMips: ");
            sb.Append(HasMipMaps);
            sb.Append(", Layout: ");
            sb.Append(Enum.GetName(typeof(PixelInternalFormat), Layout));
            sb.Append(", WrapMode: ");
            sb.Append(Enum.GetName(typeof(TextureWrapMode), WrapMode));
            sb.Append(", FilterMode: ");
            sb.Append(Enum.GetName(typeof(TextureMinFilter), FilterMode));

            sb.Append(" }");
            return sb.ToString();
        }
    }
}
