using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class ScopedQuery : Query, IDisposable
    {
        public ScopedQuery(QueryTarget type) : base(type)
        {
            BeginScope();
        }

        public void BeginScope()
        {
            if (Running)
                return;

            GL.BeginQuery(Type, Handle);
            Metrics.QueriesPerformed++;
            Running = true;
        }

        private void EndScope()
        {
            if (Running)
                GL.EndQuery(Type);
        }

        public void Dispose() => EndScope();
    }
}
