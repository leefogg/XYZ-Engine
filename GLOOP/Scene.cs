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
        public List<RenderBatch<DeferredRenderingGeoMaterial>> Batches;

        public void Render(Matrix4 projectionMatrix, Matrix4 viewMatrix)
        {
            foreach (var batch in Batches)
                foreach (var model in batch.Models)
                    model.Render(projectionMatrix, viewMatrix);
        }

        public void RenderBoundingBoxes(Matrix4 projectionMatrix, Matrix4 viewMatrix)
        {
            foreach (var batch in Batches)
                foreach (var model in batch.Models)
                    model.RenderBoundingBox(projectionMatrix, viewMatrix);

            var portalColor = new Vector4(1, 0, 0, 0.25f);
            var areaColor = new Vector4(0, 1, 0, 1);
            foreach (var area in VisibilityPortals)
            {
                var modelMatrix = Matrix4.CreateScale(area.BoundingBox.Size) * Matrix4.CreateTranslation(area.BoundingBox.Center);
                Draw.Box(projectionMatrix, viewMatrix, modelMatrix, portalColor);
            }
            foreach (var area in VisibilityAreas)
            {
                var modelMatrix = Matrix4.CreateScale(area.BoundingBox.Size) * Matrix4.CreateTranslation(area.BoundingBox.Center);
                Draw.BoundingBox(projectionMatrix, viewMatrix, modelMatrix, areaColor);
            }
        }
    }
}
