using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public static class Globals
    {
        private static int? maxLabelLength;
        public static int MaxLabelLength
        {
            get
            {
                if (!maxLabelLength.HasValue)
                    maxLabelLength = GL.GetInteger((GetPName)All.MaxLabelLength);
                return maxLabelLength.Value;
            }
        }
    }
}
