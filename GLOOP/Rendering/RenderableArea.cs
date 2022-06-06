using GLOOP.Extensions;
using GLOOP.Rendering.Debugging;
using GLOOP.Rendering.Materials;
using GLOOP.Util;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace GLOOP.Rendering
{
    public abstract class RenderableArea
    {
        public string Name { get; private set; }

        public List<Model> Models = new List<Model>();
        public List<PointLight> PointLights = new List<PointLight>();
        public List<SpotLight> SpotLights = new List<SpotLight>();

        public IReadOnlyList<Model> Occluders { get; private set; }
        public IReadOnlyList<Model> NonOccluders { get; private set; }

        protected RenderableArea(string name)
        {
            Name = name;
        }

        public void UpdateModelBatches(List<Model> occluders, List<Model> nonOccluders)
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

        public IEnumerable<PointLight> GetVisiblePointLights() 
            => PointLights.Where(light => Camera.Current.IsInsideFrustum(light.Position, light.Radius));
        public IEnumerable<SpotLight> GetVisibleSpotLights()
            => SpotLights.Where(light => Camera.Current.IsInsideFrustum(light.Position, light.Radius));
    }
}
