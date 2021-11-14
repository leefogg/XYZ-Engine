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

        public override void RenderGeometry()
        {
            base.RenderGeometry();

            foreach (var terrainPatch in Terrain)
                terrainPatch.Render();
        }

        public void RenderBoundingBoxes()
        {
            foreach (var model in Models)
                model.RenderBoundingBox();

            var portalColor = new Vector4(1, 0, 0, 0.25f);
            var areaColor = new Vector4(0, 1, 0, 1);
            foreach (var area in VisibilityPortals)
            {
                var modelMatrix = Matrix4.CreateScale(area.BoundingBox.Size) * Matrix4.CreateTranslation(area.BoundingBox.Center);
                Draw.Box( modelMatrix, portalColor);
            }
            foreach (var area in VisibilityAreas.Values)
            {
                var modelMatrix = Matrix4.CreateScale(area.BoundingBox.Size) * Matrix4.CreateTranslation(area.BoundingBox.Center);
                Draw.BoundingBox(modelMatrix, areaColor);
            }
        }
    }
}
