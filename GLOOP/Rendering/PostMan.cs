using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class PostMan
    {
        private static readonly FrameBuffer[] Pool = new FrameBuffer[2];
        private static int CurrentBufferIndex;

        public static FrameBuffer NextFramebuffer
        {
            get
            {
                CurrentBufferIndex = ++CurrentBufferIndex & 1;
                return Pool[CurrentBufferIndex];
            }
        }

        public static void Init(int width, int height, PixelInternalFormat format)
        {
            for (int i = 0; i < Pool.Length; i++)
            {
                Pool[i] = new FrameBuffer(width, height, false, format);
            }
        }
    }
}
