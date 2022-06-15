using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class TimestampQuery : Query
    {
        public TimestampQuery() : base(QueryTarget.Timestamp)
        {
        }

        public void Dispatch()
        {
            if (Running)
                return;

            GL.QueryCounter(Handle, QueryCounterTarget.Timestamp);
            Metrics.QueriesPerformed++;
            Running = true;
        }
    }
}
