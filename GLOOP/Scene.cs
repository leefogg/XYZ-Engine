using GLOOP.Rendering;
using GLOOP.Rendering.Debugging;
using GLOOP.Rendering.Materials;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public class Scene : RenderableArea
    {
        public List<Model> Terrain = new List<Model>();
        public List<VisibilityPortal> VisibilityPortals = new List<VisibilityPortal>();
        public Dictionary<string, VisibilityArea> VisibilityAreas = new Dictionary<string, VisibilityArea>();

        public Scene() : base ("World")
        {

        }

        public void RenderTerrain()
        {
            if (Terrain.Count == 0)
                return;

            using var deugGroup = new DebugGroup("Terrain");
            foreach (var terrainPatch in Terrain)
                terrainPatch.Render();
        }

        public void RenderBoundingBoxes()
        {
            foreach (var model in Models)
                model.RenderBoundingBox();
            foreach (var area in VisibilityAreas.Values)
                foreach (var model in area.Models)
                    model.RenderBoundingBox();
            foreach (var area in VisibilityPortals)
                area.RenderBoundingBox();
            foreach (var area in VisibilityAreas.Values)
                area.RenderBoundingBox();
        }
    }
}
