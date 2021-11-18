﻿using OpenTK.Graphics.OpenGL4;
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

        private Query FindNextAvailable(QueryTarget target, int startIndex = 0)
        {
            for (var i = startIndex; i < Queries.Length; i++)
            {
                if (Queries[i] == null)
                {
                    Queries[i] = new Query(target);
                    return Queries[i];
                } 
                else
                {
                    if (Queries[i].Type == target && !Queries[i].Running)
                    {
                        Queries[i].BeginScope();
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
                var oldsize = Queries.Length;
                Resize();
                return FindNextAvailable(target, oldsize);
            }

            return nextAvailable;
        }

        private void Resize()
        {
            Console.WriteLine("Out of queries. Must resize..");

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
