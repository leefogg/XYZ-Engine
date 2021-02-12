using System.Collections.Generic;
using System.Linq;

namespace GLOOP.Rendering
{
    internal class DeferredMaterialGrouper : MaterialGrouper<DeferredRenderingGeoMaterial>
    {
        internal class DeferredMaterialRenderBatch : RenderBatch<DeferredRenderingGeoMaterial>
        {
            private readonly VAO vao;
            private readonly DeferredRenderingGeoMaterial material;

            public DeferredMaterialRenderBatch(Model model)
            {
                vao = model.VAO.container;
                material = (DeferredRenderingGeoMaterial)model.Material;

                Models.Add(model);
            }
            public override void BindState()
            {
                material.Commit();
                vao.Bind();
            }

            public override bool IsSameBatch(Model model)
            {
                var mat = (DeferredRenderingGeoMaterial)model.Material;
                return vao == model.VAO.container
                    && material.DiffuseTexture == mat.DiffuseTexture
                    && material.NormalTexture == mat.NormalTexture
                    && material.SpecularTexture == mat.SpecularTexture
                    && material.IlluminationTexture == mat.IlluminationTexture;
            }
        }

        public override IEnumerable<RenderBatch<DeferredRenderingGeoMaterial>> Sort(IEnumerable<Model> models)
        {
            var batches = new List<RenderBatch<DeferredRenderingGeoMaterial>>();

            foreach (var model in models)
            {
                var added = false;
                foreach (var batch in batches)
                {
                    if (batch.IsSameBatch(model))
                    {
                        batch.Models.Add(model);
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    var newBatch = new DeferredMaterialRenderBatch(model);
                    batches.Add(newBatch);
                }
            }

            return batches;
        }
    }
}
