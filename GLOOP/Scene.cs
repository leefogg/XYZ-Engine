using GLOOP.Rendering;
using GLOOP.Rendering.Debugging;
using GLOOP.Rendering.Materials;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public class Scene
    {
        public List<Model> Models = new List<Model>();
        public List<PointLight> PointLights = new List<PointLight>();
        public List<SpotLight> SpotLights = new List<SpotLight>();
        public List<Model> Terrain = new List<Model>();
        public List<VisibilityPortal> VisibilityPortals = new List<VisibilityPortal>();
        public List<VisibilityArea> VisibilityAreas = new List<VisibilityArea>();
        public List<RenderBatch> Batches;

        public void Render()
        {
            foreach (var batch in Batches)
                foreach (var model in batch.Models)
                    model.Render();
        }

        public void RenderBoundingBoxes()
        {
            foreach (var batch in Batches)
                foreach (var model in batch.Models)
                    model.RenderBoundingBox();

            var portalColor = new Vector4(1, 0, 0, 0.25f);
            var areaColor = new Vector4(0, 1, 0, 1);
            foreach (var area in VisibilityPortals)
            {
                var modelMatrix = Matrix4.CreateScale(area.BoundingBox.Size) * Matrix4.CreateTranslation(area.BoundingBox.Center);
                Draw.Box( modelMatrix, portalColor);
            }
            foreach (var area in VisibilityAreas)
            {
                var modelMatrix = Matrix4.CreateScale(area.BoundingBox.Size) * Matrix4.CreateTranslation(area.BoundingBox.Center);
                Draw.BoundingBox(modelMatrix, areaColor);
            }
        }
    }
}
