using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public abstract class MaterialGrouper<Mat> where Mat : Material
    {
        public abstract IEnumerable<RenderBatch<Mat>> Sort(IEnumerable<Model> models);
    }
}
