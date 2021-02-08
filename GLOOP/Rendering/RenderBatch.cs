using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public abstract class RenderBatch<T> where T : Material
    {
        public List<Model> Models = new List<Model>();

        public abstract void BindState();
        public abstract bool IsSameBatch(Model model);
    }
}
