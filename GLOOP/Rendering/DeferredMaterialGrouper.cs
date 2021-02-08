using System.Collections.Generic;
using System.Linq;

namespace GLOOP.Rendering
{
    internal class DeferredMaterialGrouper<M> : MaterialGrouper<DeferredRenderingGeoMaterial, M>
    {
        internal class Group<M>
        {
            private readonly DeferredRenderingGeoMaterial material;
            public List<M> Items = new List<M>();

            public Group(DeferredRenderingGeoMaterial mat)
            {
                material = mat;
            }

            public bool IsSame(DeferredRenderingGeoMaterial mat) 
                => material.DiffuseTexture == mat.DiffuseTexture 
                && material.NormalTexture == mat.NormalTexture 
                && material.SpecularTexture == mat.SpecularTexture 
                && material.IlluminationTexture == mat.IlluminationTexture;
        }

        public DeferredMaterialGrouper()
        {
        }

        public override IEnumerable<M> Sort(IEnumerable<(DeferredRenderingGeoMaterial, M)> materials)
        {
            var groups = new List<Group<M>>();

            foreach (var set in materials)
            {
                var material = set.Item1;
                var added = false;
                foreach (var group in groups)
                {
                    if (group.IsSame(set.Item1))
                    {
                        group.Items.Add(set.Item2);
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    var newGroup = new Group<M>(material);
                    newGroup.Items.Add(set.Item2);
                    groups.Add(newGroup);
                }
            }

            return groups.SelectMany(g => g.Items);
        }
    }
}
