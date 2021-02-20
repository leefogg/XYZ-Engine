using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class Query : IDisposable
    {
        private readonly int Handle = GL.GenQuery();
        private QueryTarget Type;
        public bool Running { get; private set; }

        public Query(QueryTarget type)
        {
            BeginScope(type);
        }

        public void BeginScope(QueryTarget type)
        {
            if (Running)
                return;

            Type = type;
            GL.BeginQuery(Type, Handle);
            Running = true;
        }

        public void EndScope()
        {
            if (Running)
                GL.EndQuery(Type);
        }

        public bool IsResultAvailable()
        {
            if (!Running)
                return true;

            GL.GetQueryObject(Handle, GetQueryObjectParam.QueryResultAvailable, out int avialable);

            Running = avialable == (int)All.False;

            return !Running;
        }

        public int GetResult()
        {
            GL.GetQueryObject(Handle, GetQueryObjectParam.QueryResult, out int result);
            return result;
        }

        public void Dispose() => EndScope();
    }
}
