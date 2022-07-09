using GLOOP.Extensions;
using GLOOP.Rendering;
using GLOOP.Rendering.Debugging;
using GLOOP.Rendering.Materials;
using GLOOP.Util;
using GLOOP.Util.Structures;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace GLOOP
{
    public class Scene : RenderableArea
    {
        private static readonly int commandSize = Marshal.SizeOf<DrawElementsIndirectData>();

        public List<Model> Terrain = new List<Model>();
        public List<VisibilityPortal> VisibilityPortals = new List<VisibilityPortal>();
        public Dictionary<string, VisibilityArea> VisibilityAreas = new Dictionary<string, VisibilityArea>();

        // Rendering stuff
        private Buffer<DrawElementsIndirectData> DrawIndirectBuffer;
        private Buffer<GPUModel> ModelsBuffer;
        private Buffer<GPUPointLight> PointLightsBuffer;
        private Buffer<GPUSpotLight> SpotLightsBuffer;
        private Buffer<int> PointLightIndexBuffer, SpotLightIndexBuffer;
        private readonly FastList<DrawElementsIndirectData> ScratchDrawCommands = new FastList<DrawElementsIndirectData>(1024 * 4);
        private readonly FastList<GPUModel> ScratchGPUModels = new FastList<GPUModel>(1024 * 4);
        private readonly Ring<FastList<int>> VisibleSpotLights = new Ring<FastList<int>>(PowerOfTwo.Two, i => new FastList<int>(1024));
        private readonly Ring<FastList<int>> VisiblePointLights = new Ring<FastList<int>>(PowerOfTwo.Two, i => new FastList<int>(1024));
        private readonly Ring<List<Model>> VisibleOccluders = new Ring<List<Model>>(PowerOfTwo.Two, NewListOfModels);
        private readonly Ring<List<Model>> VisibleNonOccluders = new Ring<List<Model>>(PowerOfTwo.Two, NewListOfModels);
        private readonly Ring<List<Model>> VisibleTerrain = new Ring<List<Model>>(PowerOfTwo.Two, NewListOfModels);
        private readonly List<RenderBatch> NonOccluderBatches = new List<RenderBatch>();
        private readonly List<RenderBatch> OccluderBatches = new List<RenderBatch>();

#if DEBUG
        // Debug
        private IList<SpotLight> AllSpotLights;
        private IList<PointLight> AllPointLights;
#endif

        private static List<Model> NewListOfModels(int i) => new List<Model>();

        public Scene() : base("World")
        {

        }

        private void NextBuffers()
        {
            VisibleOccluders.MoveNext();
            VisibleNonOccluders.MoveNext();
            VisiblePointLights.MoveNext();
            VisibleSpotLights.MoveNext();
            VisibleTerrain.MoveNext();
        }

        public void RenderTerrain()
        {
            if (VisibleTerrain.Current.Count == 0)
                return;

            using var deugGroup = new DebugGroup("Terrain");
            foreach (var terrainPatch in VisibleTerrain.Current)
                terrainPatch.Render();
        }

        public void SetupBuffers()
        {
            CreateLightBuffers();
            CreateModelBuffers();
        }

        private void CreateLightBuffers()
        {
            var totalPointLights = PointLights.Count + VisibilityAreas.Values.Sum(area => area.PointLights.Count);
            PointLightsBuffer = new Buffer<GPUPointLight>(
                totalPointLights,
                BufferTarget.ShaderStorageBuffer,
                BufferUsageHint.StaticDraw,
                "PointLights"
            );
            PointLightIndexBuffer = new Buffer<int>(
                Enumerable.Range(0, 512).ToArray(),
                BufferTarget.UniformBuffer,
                BufferUsageHint.StreamDraw,
                "Point Light Indicies"
            );

            var totalSpotLights = SpotLights.Count + VisibilityAreas.Values.Sum(area => area.SpotLights.Count);
            SpotLightsBuffer = new Buffer<GPUSpotLight>(
                totalSpotLights,
                BufferTarget.ShaderStorageBuffer,
                BufferUsageHint.StaticDraw,
                "SpotLights"
            );

            SpotLightIndexBuffer = new Buffer<int>(
                Enumerable.Range(0, 512).ToArray(),
                BufferTarget.UniformBuffer,
                BufferUsageHint.StreamDraw,
                "Spot Light Indicies"
            );


            PopulatePointLightsBuffer(PointLights.AppendRange(VisibilityAreas.Values.SelectMany(area => area.PointLights)).Distinct().ToArray());
            PopulateSpotLightsBuffer(SpotLights.AppendRange(VisibilityAreas.Values.SelectMany(area => area.SpotLights)).Distinct().ToArray());
        }

        private void CreateModelBuffers()
        {
            var numModels = Models.Count + VisibilityAreas.Values.Sum(area => area.Models.Count);
            DrawIndirectBuffer = new Buffer<DrawElementsIndirectData>(
                numModels,
                BufferTarget.DrawIndirectBuffer,
                BufferUsageHint.StreamDraw,
                "DrawCommands"
            );
            ModelsBuffer = new Buffer<GPUModel>(
                numModels,
                BufferTarget.ShaderStorageBuffer,
                BufferUsageHint.StreamDraw,
                "Models"
            );
        }

        public void UpdateVisibility(IList<RenderableArea> areas)
        {
            var visibleAreas = areas;
            visibleAreas.Add(this);
            var visibleOccluders    = VisibleOccluders.Peek();
            var visibleNonOccluders = VisibleNonOccluders.Peek();
            var visibleTerrain      = VisibleTerrain.Peek();
            var visiblePointLights  = VisiblePointLights.Peek();
            var visibleSpotLights   = VisibleSpotLights.Peek();

            visibleOccluders.Clear();
            visibleNonOccluders.Clear();
            foreach (var room in visibleAreas)
                room.AddVisibleModels(visibleOccluders, visibleNonOccluders);

            visiblePointLights.Clear();
            visibleSpotLights.Clear();
            foreach (var room in visibleAreas)
            {
                var visibleLights = room.PointLights
                    .Where(light => Camera.Current.IntersectsFrustum(light.Position, light.Radius * 2))
                    .Select(light => light.PointLightIndex);
                visiblePointLights.AddRange(visibleLights);
            }
            foreach (var room in visibleAreas)
            {
                var visibleLights = room.SpotLights
                    .Where(light =>Camera.Current.IntersectsFrustum(light.Position, light.Radius * 2))
                    .Select(light => light.SpotLightIndex);
                visibleSpotLights.AddRange(visibleLights);
            }

            visibleTerrain.Clear();
            visibleTerrain.AddRange(
                Terrain
                .Where(terrain => Camera.Current.IntersectsFrustum(terrain.BoundingBox.ToSphereBounds()))
                .OrderBy(terrain => (terrain.Transform.Position - Camera.Current.Position).LengthSquared)
            );

            NextBuffers();
        }

        public void UpdateBuffers()
        {
            BatchModels(VisibleOccluders.Current, OccluderBatches);
            BatchModels(VisibleNonOccluders.Current, NonOccluderBatches);

            ScratchDrawCommands.Clear();
            ScratchGPUModels.Clear();
            AddModelData(OccluderBatches,    ScratchDrawCommands, ScratchGPUModels);
            AddModelData(NonOccluderBatches, ScratchDrawCommands, ScratchGPUModels);
            if (ScratchDrawCommands.Count > 0)
            {
                DrawIndirectBuffer.Update(ScratchDrawCommands.Elements, ScratchDrawCommands.Count);
                ModelsBuffer.Update(ScratchGPUModels.Elements, ScratchGPUModels.Count);
            }

            PointLightIndexBuffer.Update(VisiblePointLights.Current.Elements, VisiblePointLights.Current.Count);
            SpotLightIndexBuffer.Update(VisibleSpotLights.Current.Elements, VisibleSpotLights.Current.Count);
        }

        private void PopulateSpotLightsBuffer(IList<SpotLight> visibleLights)
        {
            if (visibleLights.Count == 0)
                return;

            var lights = new GPUSpotLight[SpotLight.NumLights];
            foreach (var light in visibleLights)
                lights[light.SpotLightIndex] = CreateLight(light);
#if DEBUG
            AllSpotLights = visibleLights;
#endif
            SpotLightsBuffer.Update(lights);
        }

        private void PopulatePointLightsBuffer(IList<PointLight> visibleLights)
        {
            if (visibleLights.Count == 0)
                return;

            var lights = new GPUPointLight[PointLight.NumLights];
            foreach (var light in visibleLights)
                lights[light.PointLightIndex] = CreateLight(light);
#if DEBUG
            AllPointLights = visibleLights;
#endif
            PointLightsBuffer.Update(lights);
        }

        private static GPUPointLight CreateLight(PointLight light)
        {
            light.GetLightingScalars(out var diffuseScalar, out var specularScalar);
            return new GPUPointLight(
                light.Position,
                light.Color,
                light.Brightness,
                light.Radius * 2, // TODO: Should not be doubled, need to fix brightness
                light.FalloffPower,
                diffuseScalar,
                specularScalar
            );
        }
        private static GPUSpotLight CreateLight(SpotLight light)
        {
            light.GetLightingScalars(out var diffuseScalar, out var specularScalar);
            var modelMatrix = MathFunctions.CreateModelMatrix(light.Position, light.Rotation, Vector3.One);
            var dir = Matrix4.CreateFromQuaternion(light.Rotation) * new Vector4(0, 0, 1, 1);

            GetLightVars(light, out var aspect, out var scale);

            var rot = light.Rotation.ToEulerAngles();
            rot.X = MathHelper.RadiansToDegrees(rot.X);
            rot.Y = MathHelper.RadiansToDegrees(rot.Y);
            rot.Z = MathHelper.RadiansToDegrees(rot.Z);
            var viewMatrix = MathFunctions.CreateViewMatrix(light.Position, rot);
            var projectionMatrix = new Matrix4();
            MathFunctions.CreateProjectionMatrix(aspect, light.FOV, light.ZNear, light.Radius, ref projectionMatrix);
            var viewProjection = new Matrix4();
            MatrixExtensions.Multiply(projectionMatrix, viewMatrix, ref viewProjection);

            return new GPUSpotLight(
                modelMatrix,
                light.Position,
                light.Color,
                dir.Xyz,
                scale * 2,
                aspect,
                light.Brightness,
                light.Radius * 2,
                light.FalloffPower,
                light.AngularFalloffPower,
                light.FOV,
                diffuseScalar,
                specularScalar,
                viewProjection
            );
        }

        private static void GetLightVars(SpotLight light, out float ar, out Vector3 scale)
        {
            ar = light.AspectRatio;
            var deg2Rad = 0.0174533f;
            var halfHeight = (float)Math.Tan(deg2Rad * (light.FOV / 2f));
            var halfWidth = halfHeight * ar;
            var far = light.Radius;
            var xf = halfWidth * far;
            var yf = halfHeight * far;
            scale = new Vector3(xf, yf, -far);
        }

        private List<RenderBatch> BatchModels(IEnumerable<Model> models, List<RenderBatch> batches)
        {
            using var profiler = EventProfiler.Profile("Batching");

            GroupBy(models.OrderBy(m => m.Material.Shader.Handle), batches, SameRenderBatch);
            batches.ForEach(batch => batch.Models = batch.Models.OrderBy(model => (model.Transform.Position - Camera.Current.Position).LengthSquared).ToList());

            return batches;
        }

        private static void GroupBy(IEnumerable<Model> models, List<RenderBatch> batches, Func<Model, Model, bool> comparer)
        {
            batches.Clear();

            foreach (var model in models)
            {
                var foundBatch = false;
                foreach (var batch in batches)
                {
                    if (comparer(batch.Models[0], model))
                    {
                        foundBatch = true;
                        batch.Models.Add(model);
                        break;
                    }
                }

                if (!foundBatch)
                    batches.Add(new RenderBatch(new[] { model }));
            }
        }

        private static bool SameRenderBatch(Model a, Model b)
        {
            var mat1 = (DeferredRenderingGeoMaterial)a.Material;
            var mat2 = (DeferredRenderingGeoMaterial)b.Material;
            return a.VAO.Container.VAOHandle == b.VAO.Container.VAOHandle
                && a.Material.Shader.Handle == b.Material.Shader.Handle
                && mat1.SameTextures(mat2);
        }

        private void AddModelData(
            IEnumerable<RenderBatch> batches,
            FastList<DrawElementsIndirectData> drawIndirectDest,
            FastList<GPUModel> modelDest)
        {
            foreach (var batch in batches)
            {
                foreach (var model in batch.Models)
                {
                    var mat = (DeferredRenderingGeoMaterial)model.Material;
                    modelDest.Add(new GPUModel(
                        model.Transform.Matrix,
                        new GPUDeferredGeoMaterial(mat.AlbedoColourTint, mat.IlluminationColor, mat.TextureRepeat, mat.TextureOffset, 1, mat.HasWorldpaceUVs)
                    ));

                    var command = model.VAO.Description;
                    drawIndirectDest.Add(new DrawElementsIndirectData(
                        command.NumIndexes,
                        command.FirstIndex / sizeof(ushort),
                        command.BaseVertex,
                        command.NumInstances,
                        (uint)drawIndirectDest.Count
                    ));
                }
            }
        }

        public void RenderModels()
        {
            DrawIndirectBuffer.Bind();
            ModelsBuffer.Bind(1);
            var drawCommandPtr = (IntPtr)0;
            RenderOccluderGeometry(ref drawCommandPtr);
            RenderNonOccluderGeometry(ref drawCommandPtr);
        }

        public void RenderOccluderGeometry(ref IntPtr drawCommandPtr)
        {
            using var timer = new DebugGroup("Occluders");
            if (OccluderBatches != null && OccluderBatches.Count > 0)
                MultiDrawIndirect(OccluderBatches, ref drawCommandPtr);
        }

        public void RenderNonOccluderGeometry(ref IntPtr drawCommandPtr)
        {
            using var timer = new DebugGroup("Non Occluders");
            if (NonOccluderBatches != null && NonOccluderBatches.Count > 0)
                MultiDrawIndirect(NonOccluderBatches, ref drawCommandPtr);
        }

        private void MultiDrawIndirect(IList<RenderBatch> batches, ref IntPtr drawCommandPtr)
        {
            foreach (var batch in batches)
            {
                var batchSize = batch.Models.Count;

                batch.BindState();

                GL.MultiDrawElementsIndirect(
                    PrimitiveType.Triangles,
                    DrawElementsType.UnsignedShort,
                    drawCommandPtr,
                    batchSize,
                    0
                );

                drawCommandPtr += batchSize * commandSize;

                Metrics.ModelsDrawn += batchSize;
                Metrics.RenderBatches++;
            }
        }

        public void RenderLights(
            Material singleColorMaterial,
            FrustumMaterial frustumMaterial,
            Shader pointLightShader, 
            Shader spotLightShader, 
            Texture2D[] gbuffers,
            bool debugLights)
        {
            {
                var numCulledPointLights = VisiblePointLights.Current.Count;
                if (numCulledPointLights > 0)
                {
                    using (new DebugGroup("Point Lights"))
                    {
                        PointLightsBuffer.Bind(1);
                        PointLightIndexBuffer.Bind(1);

                        var shader = pointLightShader;
                        shader.Use();
                        Texture.Use(gbuffers, TextureUnit.Texture0);
                        shader.Set("diffuseTex", TextureUnit.Texture0);
                        shader.Set("positionTex", TextureUnit.Texture1);
                        shader.Set("normalTex", TextureUnit.Texture2);
                        shader.Set("specularTex", TextureUnit.Texture3);
                        shader.Set("camPos", Camera.Current.Position);
                        //TODO: Could render a 2D circle in screenspace instead of a sphere

                        Primitives.Sphere.Draw(numInstances: numCulledPointLights);
                        Metrics.LightsDrawn += numCulledPointLights;

                        // Debug light spheres
#if DEBUG
                        if (debugLights)
                        {
                            foreach (var index in VisiblePointLights.Current.ToSpan())
                            {
                                var light = AllPointLights[index];
                                var modelMatrix = MathFunctions.CreateModelMatrix(light.Position, Quaternion.Identity, new Vector3(light.Radius));
                                singleColorMaterial.ModelMatrix = modelMatrix;
                                singleColorMaterial.Commit();
                                Primitives.Sphere.Draw(PrimitiveType.Lines);
                            }
                        }
#endif
                    }
                }
            }

            {
                var numCulledSpotLights = VisibleSpotLights.Current.Count;
                if (numCulledSpotLights > 0)
                {
                    using (new DebugGroup("Spot Lights"))
                    {
                        SpotLightsBuffer.Bind(1);
                        SpotLightIndexBuffer.Bind(1);

                        var shader = spotLightShader;
                        shader.Use();
                        Texture.Use(gbuffers, TextureUnit.Texture0);
                        shader.Set("diffuseTex", TextureUnit.Texture0);
                        shader.Set("positionTex", TextureUnit.Texture1);
                        shader.Set("normalTex", TextureUnit.Texture2);
                        shader.Set("specularTex", TextureUnit.Texture3);
                        shader.Set("camPos", Camera.Current.Position);

                        Primitives.Frustum.Draw(numInstances: numCulledSpotLights);
                        Metrics.LightsDrawn += numCulledSpotLights;
#if DEBUG
                        if (debugLights)
                        {
                            var material = frustumMaterial;
                            foreach (var index in VisibleSpotLights.Current.ToSpan())
                            {
                                var light = AllSpotLights[index];
                                var modelMatrix = MathFunctions.CreateModelMatrix(light.Position, light.Rotation, Vector3.One);
                                GetLightVars(light, out var aspect, out var scale);
                                material.AspectRatio = aspect;
                                material.Scale = scale;
                                material.ModelMatrix = modelMatrix;
                                material.Commit();
                                Primitives.Frustum.Draw(PrimitiveType.Lines);
                            }
                        }
#endif
                    }
                }
            }
        }
    }
}
