﻿using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class Query : IDisposable
    {
        private readonly int Handle = GL.GenQuery();
        public readonly QueryTarget Type;
        public bool Running { get; private set; }

        public Query(QueryTarget type)
        {
            Type = type;
            BeginScope();
        }

        public void BeginScope()
        {
            if (Running)
                return;

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
            Running = false;
            return result;
        }

        public void Dispose() => EndScope();
    }
}
