using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public abstract class MaterialGrouper<T, M> where T : Material
    {
        public abstract IEnumerable<M> Sort(IEnumerable<(T, M)> materials);
    }
}
