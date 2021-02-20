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
            for (var i = 0; i < initialSize; i++)
                Queries[i] = new Query();
        }

        private Query FindNextAvailable()
        {
            foreach (var query in Queries)
                if (!query.Running)
                    return query;
            return null;
        }

        private Query GetQuery()
        {
            var nextAvailable = FindNextAvailable();
            if (nextAvailable == null)
                Resize();
            return FindNextAvailable();
        }

        private void Resize()
        {
            var currentQuries = Queries;
            Queries = new Query[Queries.Length * 2];

            var i = 0;
            for (; i < currentQuries.Length; i++)
                Queries[i] = currentQuries[i];
            for (; i < Queries.Length; i++)
                Queries[i] = new Query();
        }

        public Query BeginScope(QueryTarget target)
        {
            var query = GetQuery();
            query.BeginScope(target);
            return query;
        }
    }
}
