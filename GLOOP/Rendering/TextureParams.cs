using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class TextureParams
    {
        public bool GenerateMips = false;
        public PixelInternalFormat InternalFormat = PixelInternalFormat.Rgb;
        public PixelFormat PixelFormat = PixelFormat.Rgb;
        public TextureMinFilter MinFilter = TextureMinFilter.Linear;
        public TextureMinFilter MagFilter = TextureMinFilter.Linear;
        public TextureWrapMode WrapMode = TextureWrapMode.Repeat;
        public IntPtr Data = new IntPtr();
        public int CompressedDataLength;
        public string Name;
    }
}
