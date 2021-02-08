﻿using GLOOP.Rendering;
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
        public List<RenderBatch<DeferredRenderingGeoMaterial>> Batches;

        public void Render(Matrix4 projectionMatrix, Matrix4 viewMatrix)
        {
            foreach (var batch in Batches)
            {
                foreach (var model in batch.Models)
                {
                    model.Render(projectionMatrix, viewMatrix);
                    // model.RenderBoundingBox(projectionMatrix, viewMatrix);
                }
            }
        }
    }
}
