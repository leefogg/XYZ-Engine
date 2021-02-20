using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class QueryPool
    {
        private Query[] Queries;

        public QueryPool(int initialSize)
        {
            Queries = new Query[initialSize];
        }

        private Query FindNextAvailable(QueryTarget target)
        {
            for (var i = 0; i < Queries.Length; i++)
            {
                if (Queries[i] == null)
                {
                    Queries[i] = new Query(target);
                    return Queries[i];
                } 
                else
                {
                    if (!Queries[i].Running)
                    {
                        Queries[i].BeginScope(target);
                        return Queries[i];
                    }
                }
            }

            return null;
        }

        private Query BeginQuery(QueryTarget target)
        {
            var nextAvailable = FindNextAvailable(target);
            if (nextAvailable == null)
            {
                Resize();
                return FindNextAvailable(target);
            }

            return nextAvailable;
        }

        private void Resize()
        {
            var currentQuries = Queries;
            Queries = new Query[Queries.Length * 2];

            var i = 0;
            for (; i < currentQuries.Length; i++)
                Queries[i] = currentQuries[i];
        }

        public Query BeginScope(QueryTarget target)
        {
            var query = BeginQuery(target);
            return query;
        }
    }
}
