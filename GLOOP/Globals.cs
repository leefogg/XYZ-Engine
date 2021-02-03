using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public static class Globals
    {
        private static int? maxLabelLength, uniformBufferOffsetAlignment;
        public static int MaxLabelLength
        {
            get
            {
                if (!maxLabelLength.HasValue)
                    maxLabelLength = GL.GetInteger((GetPName)All.MaxLabelLength);
                return maxLabelLength.Value;
            }
        }

        public static int UniformBufferOffsetAlignment
        {
            get
            {
                if (!uniformBufferOffsetAlignment.HasValue)
                    uniformBufferOffsetAlignment = GL.GetInteger((GetPName)All.UniformBufferOffsetAlignment);
                return uniformBufferOffsetAlignment.Value;
            }
        }
    }
}
