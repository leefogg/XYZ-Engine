using System.Collections.Generic;

namespace GLOOP.Rendering
{
    public abstract class RenderableArea
    {
        public string Name { get; private set; }

        public List<Model> Models = new List<Model>();
        public List<PointLight> PointLights = new List<PointLight>();
        public List<SpotLight> SpotLights = new List<SpotLight>();

        protected RenderableArea(string name)
        {
            Name = name;
        }

        public void AddVisibleModels(List<Model> occluders, List<Model> nonOccluders)
        {
            foreach (var model in Models)
            {
                if (Camera.Current.IntersectsFrustum(model.BoundingBox))
                {
                    var list = model.IsOccluder ? occluders : nonOccluders;
                    list.Add(model);
                }
            }
        }
    }
}
