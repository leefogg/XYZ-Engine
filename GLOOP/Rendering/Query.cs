using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public abstract class Query
    {
        public readonly int Handle = GL.GenQuery();
        public readonly QueryTarget Type;
        public bool Running { get; protected set; }

        protected Query(QueryTarget type)
        {
            Type = type;
        }

        public bool IsResultAvailable()
        {
            if (!Running)
                return true;

            GL.GetQueryObject(Handle, GetQueryObjectParam.QueryResultAvailable, out int available);

            Running = available == (int)All.False;

            return !Running;
        }

        public long GetResult()
        {
            GL.GetQueryObject(Handle, GetQueryObjectParam.QueryResult, out long result);
            Running = false;
            return result;
        }

        public override string ToString() => $"ID: {Handle}, Running:{Running}";
    }
}
