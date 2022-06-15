using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class ConditionalRender : IDisposable
    {
        public ConditionalRender(ScopedQuery pixelQuery, ConditionalRenderType type)
        {
            System.Diagnostics.Debug.Assert(pixelQuery.Type == QueryTarget.AnySamplesPassed);
            GL.BeginConditionalRender(pixelQuery.Handle, type);
        }

        public void Dispose()
        {
            GL.EndConditionalRender();
        }
    }
}
