using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Diagnostics;
using System.Numerics;

namespace GLOOP.Rendering
{
    public class QueryPool
    {
        private ScopedQuery[] Queries;

        public QueryPool(int initialSize)
        {
            Queries = new ScopedQuery[initialSize];
        }

        private ScopedQuery FindNextAvailable(QueryTarget target, int startIndex = 0)
        {
            for (var i = startIndex; i < Queries.Length; i++)
            {
                if (Queries[i] == null)
                {
                    Queries[i] = new ScopedQuery(target);
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

        private ScopedQuery BeginQuery(QueryTarget target)
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
            Console.WriteLine("Out of queries. Must resize");

            var currentQuries = Queries;
            Queries = new ScopedQuery[Queries.Length * 2];

            var i = 0;
            for (; i < currentQuries.Length; i++)
                Queries[i] = currentQuries[i];
        }

        public ScopedQuery BeginScope(QueryTarget target)
        {
            var query = BeginQuery(target);
            return query;
        }

        [Conditional("DEBUG")]
        public void DrawWindow(string name)
        {
            if (!ImGui.Begin(name))
                return;

            var boxSize = new Vector2(10, 10);
            var padding = 4;

            var dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetWindowPos();
            var windowSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
            pos += ImGui.GetWindowContentRegionMin();

            for (int i = 0; i < Queries.Length; i++)
            {
                var query = Queries[i];

                var borderColor = 0xFFFFFFFF;
                var fillColor = 0xFF000000;
                if (query == null)
                {
                    borderColor = 0xFF303030;
                }
                else
                {
                    if (query.Running)
                        fillColor = 0xFF0000FF;
                    else if (query.IsResultAvailable())
                        fillColor = 0xFF00FFFF;
                    else
                        fillColor = 0xFF00FF00;
                }

                dl.AddRect(pos, pos + boxSize, borderColor);
                dl.AddRectFilled(pos + Vector2.One, pos + boxSize - Vector2.One, fillColor);
                pos.X += boxSize.X;
                pos.X += padding;
            }

            ImGui.End();
        }
    }
}
