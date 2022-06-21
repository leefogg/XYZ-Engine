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
