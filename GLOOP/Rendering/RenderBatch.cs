using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class RenderBatch<T> where T : Material
    {
        public List<Model> Models = new List<Model>();

        public RenderBatch(IEnumerable<Model> models)
        {
            Models.AddRange(models);
        }

        public virtual void BindState()
        {
            Models[0].Material.Commit();
            Models[0].VAO.container.Bind();
        }
    }
}
