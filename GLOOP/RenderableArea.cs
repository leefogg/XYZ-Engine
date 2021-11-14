using GLOOP.Rendering;
using GLOOP.Rendering.Materials;
using GLOOP.Tests.Assets.Shaders;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace GLOOP
{
    public abstract class RenderableArea
    {
        private const int maxLights = 200;

        [StructLayout(LayoutKind.Explicit, Size = 48)]
        private struct GPUPointLight
        {
            [FieldOffset(0)] Vector3 position;
            [FieldOffset(16)] Vector3 color;
            [FieldOffset(28)] float brightness;
            [FieldOffset(32)] float radius;
            [FieldOffset(36)] float falloffPow;
            [FieldOffset(44)] float diffuseScalar;
            [FieldOffset(44)] float specularScalar;

            public GPUPointLight(
                Vector3 position,
                Vector3 color,
                float brightness,
                float radius,
                float falloffPow,
                float diffuseScalar,
                float specularScalar
            )
            {
                this.position = position;
                this.color = color;
                this.brightness = brightness;
                this.radius = radius;
                this.falloffPow = falloffPow;
                this.diffuseScalar = diffuseScalar;
                this.specularScalar = specularScalar;
            }
        };

        [StructLayout(LayoutKind.Explicit, Size = 224)]
        private struct GPUSpotLight
        {
            [FieldOffset(0)] Matrix4 modelMatrix;
            [FieldOffset(64)] Vector3 position;
            [FieldOffset(80)] Vector3 color;
            [FieldOffset(96)] Vector3 direction;
            [FieldOffset(112)] Vector3 scale;
            [FieldOffset(124)] float aspectRatio;
            [FieldOffset(128)] float brightness;
            [FieldOffset(132)] float radius;
            [FieldOffset(136)] float falloffPow;
            [FieldOffset(140)] float angularFalloffPow;
            [FieldOffset(144)] float FOV;
            [FieldOffset(148)] float diffuseScalar;
            [FieldOffset(152)] float specularScalar;
            [FieldOffset(160)] Matrix4 ViewProjection;

            public GPUSpotLight(
                Matrix4 modelMatrix,
                Vector3 position,
                Vector3 color,
                Vector3 direction,
                Vector3 scale,
                float ar,
                float brightness,
                float radius,
                float falloffPow,
                float angularFalloffPow,
                float fov,
                float diffuseScalar,
                float specularScalar,
                Matrix4 ViewProjection
            )
            {
                this.modelMatrix = modelMatrix;
                this.position = position;
                this.color = color;
                this.direction = direction;
                this.scale = scale;
                this.aspectRatio = ar;
                this.brightness = brightness;
                this.radius = radius;
                this.falloffPow = falloffPow;
                this.angularFalloffPow = angularFalloffPow;
                this.FOV = fov;
                this.diffuseScalar = diffuseScalar;
                this.specularScalar = specularScalar;
                this.ViewProjection = ViewProjection;
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = 64)]
        public struct GPUDeferredGeoMaterial
        {
            [FieldOffset(00)] public Vector3 IlluminationColor;
            [FieldOffset(16)] public Vector3 AlbedoColourTint;
            [FieldOffset(32)] public Vector2 TextureRepeat;
            [FieldOffset(40)] public Vector2 TextureOffset;
            [FieldOffset(48)] public float NormalStrength;
            [FieldOffset(52)] public bool IsWorldSpaceUVs;
        }

        private struct QueryPair
        {
            public Query Query;
            public StaticPixelShader shader;
        }

        public string Name { get; set; }

        public List<Model> Models = new List<Model>();
        public List<PointLight> PointLights = new List<PointLight>();
        public List<SpotLight> SpotLights = new List<SpotLight>();
        public List<RenderBatch> Batches;
        private QueryPool queryPool = new QueryPool(5);
        private List<QueryPair> GeoStageQueries = new List<QueryPair>();

        private Buffer<Matrix4> MatriciesBuffer;
        private Buffer<DrawElementsIndirectData> DrawIndirectBuffer;
        private Buffer<GPUDeferredGeoMaterial> MaterialsBuffer;
        private Buffer<GPUPointLight> PointLightsBuffer;
        private Buffer<GPUSpotLight> SpotLightsBuffer;

        protected RenderableArea(string name)
        {
            Name = name;
        }

        public void Prepare()
        {
            Batches = PrepareBatches();
            PrepareModelUBOs();
            SetupLightUBOs();
            FillPointLightsUBO();
            FillSpotLightsUBO();
        }

        private List<RenderBatch> PrepareBatches()
        {
            var visibleObjects = Models;
            var occluders = visibleObjects.Where(o => o.IsOccluder);
            var notOccluders = visibleObjects.Where(o => !o.IsOccluder);
            var occluderbatches = GroupBy(occluders, SameRenderBatch)
                .OrderBy(b => b.Models[0].Material.Shader.Handle)
                .ThenBy(AverageDistanceToCamera);
            var nonOccluderbatches = GroupBy(notOccluders, SameRenderBatch)
                .OrderBy(b => b.Models[0].Material.Shader.Handle)
                .ThenBy(AverageDistanceToCamera);

            var batches = occluderbatches.ToList();
            batches.AddRange(nonOccluderbatches);

            return batches;
        }

        private void SetupLightUBOs()
        {
            if (PointLights.Any())
            {
                PointLightsBuffer = new Buffer<GPUPointLight>(
                    Math.Min(maxLights, PointLights.Count),
                    BufferTarget.UniformBuffer,
                    BufferUsageHint.StaticDraw,
                    Name + " PointLights"
                );
            }
            if (SpotLights.Any())
            {
                SpotLightsBuffer = new Buffer<GPUSpotLight>(
                    Math.Min(maxLights, SpotLights.Count),
                    BufferTarget.UniformBuffer,
                    BufferUsageHint.StaticDraw,
                    Name + " SpotLights"
                );
            }
        }

        private void FillPointLightsUBO()
        {
            if (!PointLights.Any())
                return;

            var planes = Camera.Current.GetFrustumPlanes();

            var lights = new GPUPointLight[Math.Min(maxLights, PointLights.Count)];

            var numCulledLights = 0;
            for (var i = 0; i < lights.Length; i++)
            {
                var light = PointLights[i];

                if (Camera.IsInsideFrustum(ref planes, light.Position, light.Radius))
                {
                    light.GetLightingScalars(out float diffuseScalar, out float specularScalar);
                    lights[numCulledLights++] = new GPUPointLight(
                        light.Position,
                        light.Color,
                        light.Brightness,
                        light.Radius * 2, // TODO: Should not be doubled, need to fix brightness
                        light.FalloffPower,
                        diffuseScalar,
                        specularScalar
                    );
                }
            }
            PointLightsBuffer.Update(lights);
        }

        private void FillSpotLightsUBO()
        {
            if (!SpotLights.Any())
                return;

            var planes = Camera.Current.GetFrustumPlanes();

            var culledNumSpotLights = 0;
            var lights = new GPUSpotLight[Math.Min(maxLights, SpotLights.Count)];
            for (var i = 0; i < lights.Length; i++)
            {
                var light = SpotLights[i];
                if (Camera.IsInsideFrustum(ref planes, light.Position, light.Radius))
                {
                    light.GetLightingScalars(out float diffuseScalar, out float specularScalar);
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

                    lights[culledNumSpotLights++] = new GPUSpotLight(
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
            }

            SpotLightsBuffer.Update(lights);
        }

        private void PrepareModelUBOs()
        {
            if (!Models.Any())
                return;

            var sizeOfMatrix = Marshal.SizeOf<Matrix4>();

            var modelMatricies = new List<Matrix4>();
            var drawCommands = new List<DrawElementsIndirectData>();
            var materials = new List<GPUDeferredGeoMaterial>();
            foreach (var batch in Batches)
            {
                uint i = 0;
                foreach (var model in batch.Models)
                {
                    modelMatricies.Add(model.Transform.Matrix);

                    var mat = (DeferredRenderingGeoMaterial)model.Material;
                    materials.Add(new GPUDeferredGeoMaterial()
                    {
                        AlbedoColourTint = mat.AlbedoColourTint,
                        IlluminationColor = mat.IlluminationColor,
                        IsWorldSpaceUVs = mat.HasWorldpaceUVs,
                        TextureOffset = mat.TextureOffset,
                        TextureRepeat = mat.TextureRepeat,
                        NormalStrength = 1,
                    });

                    var command = model.VAO.description;
                    drawCommands.Add(new DrawElementsIndirectData(
                        command.NumIndexes,
                        command.FirstIndex / sizeof(ushort),
                        command.BaseVertex,
                        command.NumInstances,
                        i++
                    ));
                }

                var batchMatrixSize = i * sizeOfMatrix;
                var offAlignment = batchMatrixSize % Globals.UniformBufferOffsetAlignment;
                if (offAlignment > 0)
                {
                    var bytesToAdd = Globals.UniformBufferOffsetAlignment - offAlignment;
                    var entriesToAdd = bytesToAdd / sizeOfMatrix;
                    for (i = 0; i < entriesToAdd; i++)
                    {
                        modelMatricies.Add(new Matrix4());
                        materials.Add(new GPUDeferredGeoMaterial());
                        drawCommands.Add(new DrawElementsIndirectData());
                    }
                }
            }

            DrawIndirectBuffer = new Buffer<DrawElementsIndirectData>(
                drawCommands.ToArray(),
                BufferTarget.DrawIndirectBuffer,
                BufferUsageHint.StaticDraw, 
                Name + " DrawCommands"
            );

            MatriciesBuffer = new Buffer<Matrix4>(
                modelMatricies.ToArray(), 
                BufferTarget.UniformBuffer, 
                BufferUsageHint.StreamDraw,
                Name + " ModelMatricies"
            );
            MatriciesBuffer.BindRange(0, 1);

            MaterialsBuffer = new Buffer<GPUDeferredGeoMaterial>(
                materials.ToArray(), 
                BufferTarget.ShaderStorageBuffer, 
                BufferUsageHint.StaticDraw,
                Name + " MaterialData"
            );
            MaterialsBuffer.BindRange(0, 2);
        }

        public virtual void RenderGeometry()
        {
            if (!Models.Any())
                return;

            DrawIndirectBuffer.Bind();
            MatriciesBuffer.BindRange(0, 1);
            MaterialsBuffer.BindRange(0, 2);

            MultiDrawIndirect();
        }

        public void RenderLights(
            FrustumMaterial frustumMaterial, 
            Shader SpotLightShader,
            Shader PointLightShader,
            SingleColorMaterial singleColorMaterial, 
            Texture2D[] gbuffer,
            bool debugLights)
        {
            if (PointLights.Any())
            {
                PointLightsBuffer.BindRange(0, 1);

                var shader = PointLightShader;
                shader.Use();
                Texture.Use(gbuffer, TextureUnit.Texture0);
                shader.Set("diffuseTex", TextureUnit.Texture0);
                shader.Set("positionTex", TextureUnit.Texture1);
                shader.Set("normalTex", TextureUnit.Texture2);
                shader.Set("specularTex", TextureUnit.Texture3);
                shader.Set("camPos", Camera.Current.Position);
                //TODO: Could render a 2D circle in screenspace instead of a sphere

                //Console.WriteLine(((float)culledPointLights.Count / (float)scene.PointLights.Count) * 100 + "% of point lights");
                Primitives.Sphere.Draw(numInstances: PointLights.Count);

                // Debug light spheres
                if (debugLights)
                {
                    foreach (var light in PointLights)
                    {
                        var modelMatrix = MathFunctions.CreateModelMatrix(light.Position, new OpenTK.Mathematics.Quaternion(), new Vector3(light.Radius * 2));
                        singleColorMaterial.ModelMatrix = modelMatrix;
                        singleColorMaterial.Commit();
                        Primitives.Sphere.Draw(PrimitiveType.Lines);
                    }
                }
            }

            if (SpotLights.Any())
            {
                SpotLightsBuffer.BindRange(0, 1);

                Shader shader = SpotLightShader;
                shader.Use();
                Texture.Use(gbuffer, TextureUnit.Texture0);
                shader.Set("diffuseTex", TextureUnit.Texture0);
                shader.Set("positionTex", TextureUnit.Texture1);
                shader.Set("normalTex", TextureUnit.Texture2);
                shader.Set("specularTex", TextureUnit.Texture3);
                shader.Set("camPos", Camera.Current.Position);

                //Console.WriteLine(((float)culledSpotLights.Count / (float)scene.SpotLights.Count) * 100 + "% of spot lights");
                Primitives.Frustum.Draw(numInstances: SpotLights.Count);

                if (debugLights)
                {
                    var material = frustumMaterial;
                    foreach (var light in SpotLights)
                    {
                        var modelMatrix = MathFunctions.CreateModelMatrix(light.Position, light.Rotation, Vector3.One);
                        GetLightVars(light, out var aspect, out var scale);
                        material.AspectRatio = aspect;
                        material.Scale = scale;
                        material.ModelMatrix = modelMatrix;
                        material.Commit();
                        Primitives.Frustum.Draw(PrimitiveType.Lines);
                    }
                }
            }
        }

        private void MultiDrawIndirect()
        {
            var drawCommandPtr = (IntPtr)0;
            var modelMatrixPtr = 0;
            var materialPtr = 0;
            var commandSize = Marshal.SizeOf<DrawElementsIndirectData>();
            var matrixSize = Marshal.SizeOf<Matrix4>();
            var materialSize = Marshal.SizeOf<GPUDeferredGeoMaterial>();

            GeoStageQueries.Clear();
            Query runningQuery = null;
            int i = 0;
            foreach (var batch in Batches)
            {
                var batchSize = batch.Models.Count;

                var oldShader = Shader.Current;
                batch.BindState();
                if (Shader.Current != oldShader)
                {
                    if (runningQuery != null)
                        runningQuery.EndScope();
                    runningQuery = queryPool.BeginScope(QueryTarget.SamplesPassed);
                    GeoStageQueries.Add(new QueryPair()
                    {
                        Query = runningQuery,
                        shader = (StaticPixelShader)Shader.Current
                    });
                }

                MatriciesBuffer.BindRange(modelMatrixPtr, 1, batchSize * matrixSize);
                MaterialsBuffer.BindRange(materialPtr, 2, batchSize * materialSize);
                GL.MultiDrawElementsIndirect(
                    PrimitiveType.Triangles,
                    DrawElementsType.UnsignedShort,
                    drawCommandPtr,
                    batchSize,
                    0
                );

                var batchMatrixSize = batchSize * matrixSize;
                var offAlignment = batchMatrixSize % Globals.UniformBufferOffsetAlignment;
                if (offAlignment > 0)
                {
                    var bytesToAdd = Globals.UniformBufferOffsetAlignment - offAlignment;
                    var entriesToAdd = bytesToAdd / matrixSize;
                    batchSize += entriesToAdd;
                }

                drawCommandPtr += batchSize * commandSize;
                modelMatrixPtr += batchSize * matrixSize;
                materialPtr += batchSize * materialSize;
                i++;
            }

            runningQuery.EndScope();
        }

        public void BeforeFrame()
        {
            ReadbackQueries();
        }

        private void ReadbackQueries()
        {
            // TODO: Move this to end of frame
            long totalNumTextureSamples = 0;
            foreach (var pair in GeoStageQueries)
            {
                var fragments = pair.Query.GetResult();
                var avgTexReads = pair.shader.AverageSamplesPerFragment;
                var texWrites = pair.shader.NumOutputTargets;
                totalNumTextureSamples += fragments * avgTexReads * texWrites;
            }
            //Console.WriteLine(totalNumTextureSamples + " Texture samples");
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

        private static int AverageDistanceToCamera(RenderBatch batch)
        {
            return (int)batch.Models.Average(model => (model.Transform.Position - Camera.Current.Position).Length);
        }

        private static bool SameRenderBatch(Model a, Model b)
        {
            var mat1 = (DeferredRenderingGeoMaterial)a.Material;
            var mat2 = (DeferredRenderingGeoMaterial)b.Material;
            return a.VAO.container.Handle == b.VAO.container.Handle
                && mat1.DiffuseTexture == mat2.DiffuseTexture
                && mat1.SpecularTexture == mat2.SpecularTexture
                && mat1.NormalTexture == mat2.NormalTexture
                && mat1.IlluminationColor == mat2.IlluminationColor;
        }

        private static List<RenderBatch> GroupBy(IEnumerable<Model> models, Func<Model, Model, bool> comparer)
        {
            var batches = new List<RenderBatch>();

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

            return batches;
        }

    }
}
