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
        public List<Box3> Areas = new List<Box3>();
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

            foreach (var area in Areas)
            {
                var modelMatrix = Matrix4.CreateScale(area.Size) * Matrix4.CreateTranslation(area.Center);
                Draw.Box(projectionMatrix, viewMatrix, modelMatrix, Vector4.One);
            }
        }
    }
}
