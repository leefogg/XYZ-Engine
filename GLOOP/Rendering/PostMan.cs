using GLOOP.Util;
using GLOOP.Util.Structures;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class PostMan
    {
        private static Ring<FrameBuffer> Ring;

        public static FrameBuffer NextFramebuffer => Ring.Next;

        public static void Init(int width, int height, PixelInternalFormat format)
        {
            Ring = new Ring<FrameBuffer>(PowerOfTwo.Two, i => new FrameBuffer(width, height, false, format));
        }
    }
}
