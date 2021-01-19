using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class TextureSlice
    {
        public readonly TextureArray Texture;
        public readonly ushort Slice;

        public TextureSlice(TextureArray texture, ushort slice)
        {
            Texture = texture;
            Slice = slice;
        }
    }
}
