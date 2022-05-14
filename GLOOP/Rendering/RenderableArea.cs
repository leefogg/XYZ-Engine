﻿using GLOOP.Extensions;
using GLOOP.Rendering.Debugging;
using GLOOP.Rendering.Materials;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace GLOOP.Rendering
{
    public abstract class RenderableArea
    {
        private const int maxLights = 200;

        [StructLayout(LayoutKind.Explicit, Size = 48)]
        private readonly struct GPUPointLight
        {
            [FieldOffset(00)] readonly Vector3 position;
            [FieldOffset(12)] readonly float brightness;
            [FieldOffset(16)] readonly Vector3 color;
            [FieldOffset(28)] readonly float radius;
            [FieldOffset(32)] readonly float falloffPow;
            [FieldOffset(36)] readonly float diffuseScalar;
            [FieldOffset(40)] readonly float specularScalar;

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
        private readonly struct GPUSpotLight
        {
            [FieldOffset(0)] readonly Matrix4 modelMatrix;
            [FieldOffset(64)] readonly Vector3 position;
            [FieldOffset(80)] readonly Vector3 color;
            [FieldOffset(96)] readonly Vector3 direction;
            [FieldOffset(112)] readonly Vector3 scale;
            [FieldOffset(124)] readonly float aspectRatio;
            [FieldOffset(128)] readonly float brightness;
            [FieldOffset(132)] readonly float radius;
            [FieldOffset(136)] readonly float falloffPow;
            [FieldOffset(140)] readonly float angularFalloffPow;
            [FieldOffset(144)] readonly float FOV;
            [FieldOffset(148)] readonly float diffuseScalar;
            [FieldOffset(152)] readonly float specularScalar;
            [FieldOffset(160)] readonly Matrix4 ViewProjection;

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
                aspectRatio = ar;
                this.brightness = brightness;
                this.radius = radius;
                this.falloffPow = falloffPow;
                this.angularFalloffPow = angularFalloffPow;
                FOV = fov;
                this.diffuseScalar = diffuseScalar;
                this.specularScalar = specularScalar;
                this.ViewProjection = ViewProjection;
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = 64)]
        public readonly struct GPUDeferredGeoMaterial
        {
            [FieldOffset(00)] public readonly Vector3 IlluminationColor;
            [FieldOffset(16)] public readonly Vector3 AlbedoColourTint;
            [FieldOffset(32)] public readonly Vector2 TextureRepeat;
            [FieldOffset(40)] public readonly Vector2 TextureOffset;
            [FieldOffset(48)] public readonly float NormalStrength;
            [FieldOffset(52)] public readonly bool IsWorldSpaceUVs;

            public GPUDeferredGeoMaterial(Vector3 illuminationColor, Vector3 albedoColourTint, Vector2 textureRepeat, Vector2 textureOffset, float normalStrength, bool isWorldSpaceUVs)
            {
                IlluminationColor = illuminationColor;
                AlbedoColourTint = albedoColourTint;
                TextureRepeat = textureRepeat;
                TextureOffset = textureOffset;
                NormalStrength = normalStrength;
                IsWorldSpaceUVs = isWorldSpaceUVs;
            }
        }

        private readonly struct QueryPair
        {
            public readonly Query Query;
            public readonly StaticPixelShader shader;

            public QueryPair(Query query, StaticPixelShader shader)
            {
                Query = query;
                this.shader = shader;
            }
        }

        public string Name { get; private set; }

        public List<Model> Models = new List<Model>();
        public List<PointLight> PointLights = new List<PointLight>();
        public List<SpotLight> SpotLights = new List<SpotLight>();
        public List<RenderBatch> OccluderBatches, NonOccluderBatches;

        private Buffer<Matrix4> OccluderMatriciesBuffer, NonOccluderMatriciesBuffer;
        private Buffer<DrawElementsIndirectData> OccluderDrawIndirectBuffer, NonOccluderDrawIndirectBuffer;
        private Buffer<GPUDeferredGeoMaterial> OccluderMaterialsBuffer, NonOccluderMaterialsBuffer;

        private Buffer<GPUPointLight> PointLightsBuffer;
        private Buffer<GPUSpotLight> SpotLightsBuffer;

        protected RenderableArea(string name)
        {
            Name = name;
        }

        public void Prepare()
        {
            OccluderBatches = GetOccluderBatches();
            if (OccluderBatches.Count > 0)
            {
                var numModelsApprox = OccluderBatches.Sum(b => b.Models.Count + 3);
                OccluderDrawIndirectBuffer = new Buffer<DrawElementsIndirectData>(
                    numModelsApprox,
                    BufferTarget.DrawIndirectBuffer,
                    BufferUsageHint.StaticDraw,
                    Name + " Occluder DrawCommands"
                );
                OccluderMatriciesBuffer = new Buffer<Matrix4>(
                    numModelsApprox,
                    BufferTarget.ShaderStorageBuffer,
                    BufferUsageHint.StreamDraw,
                    Name + " Occluder ModelMatricies"
                );
                OccluderMaterialsBuffer = new Buffer<GPUDeferredGeoMaterial>(
                    numModelsApprox,
                    BufferTarget.ShaderStorageBuffer,
                    BufferUsageHint.StaticDraw,
                    Name + " Occluder MaterialData"
                );
                PrepareModelUBOs(OccluderBatches, OccluderDrawIndirectBuffer, OccluderMatriciesBuffer, OccluderMaterialsBuffer);
            }

            NonOccluderBatches = GetNonOcculuderBatches();
            if (NonOccluderBatches.Count > 0)
            {
                var numModelsApprox = NonOccluderBatches.Sum(b => b.Models.Count + 3);
                NonOccluderDrawIndirectBuffer = new Buffer<DrawElementsIndirectData>(
                    numModelsApprox,
                    BufferTarget.DrawIndirectBuffer,
                    BufferUsageHint.StaticDraw,
                    Name + " Non-Occluder DrawCommands"
                );
                NonOccluderMatriciesBuffer = new Buffer<Matrix4>(
                    numModelsApprox,
                    BufferTarget.ShaderStorageBuffer,
                    BufferUsageHint.StreamDraw,
                    Name + " Non-Occluder ModelMatricies"
                );
                NonOccluderMaterialsBuffer = new Buffer<GPUDeferredGeoMaterial>(
                    numModelsApprox,
                    BufferTarget.ShaderStorageBuffer,
                    BufferUsageHint.StaticDraw,
                    Name + " Non-Occluder MaterialData"
                );
                PrepareModelUBOs(NonOccluderBatches, NonOccluderDrawIndirectBuffer, NonOccluderMatriciesBuffer, NonOccluderMaterialsBuffer);
            }
            SetupLightUBOs();
            FillPointLightsUBO();
            FillSpotLightsUBO();
        }

        private List<RenderBatch> GetOccluderBatches()
        {
            var occluders = Models.Where(o => o.IsOccluder);
            var occluderbatches = GroupBy(occluders, SameRenderBatch)
               .OrderBy(b => b.Models[0].Material.Shader.Handle)
               .ThenBy(AverageDistanceToCamera);
            return occluderbatches.ToList();
        }

        private List<RenderBatch> GetNonOcculuderBatches()
        {
            var notOccluders = Models.Where(o => !o.IsOccluder);
            var nonOccluderbatches = GroupBy(notOccluders, SameRenderBatch)
                .OrderBy(b => b.Models[0].Material.Shader.Handle)
                .ThenBy(AverageDistanceToCamera);
            return nonOccluderbatches.ToList();
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

                //if (Camera.IsInsideFrustum(ref planes, light.Position, light.Radius))
                {
                    light.GetLightingScalars(out var diffuseScalar, out var specularScalar);
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
                //if (Camera.IsInsideFrustum(ref planes, light.Position, light.Radius))
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

        private void PrepareModelUBOs(
            IEnumerable<RenderBatch> batches,
            Buffer<DrawElementsIndirectData> drawIndirectBuffer,
            Buffer<Matrix4> matriciesBuffer,
            Buffer<GPUDeferredGeoMaterial> materialsBuffer)
        {
            if (!Models.Any())
                return;

            var modelMatricies = new List<Matrix4>();
            var drawCommands = new List<DrawElementsIndirectData>();
            var materials = new List<GPUDeferredGeoMaterial>();
            foreach (var batch in batches)
            {
                uint i = 0;
                foreach (var model in batch.Models)
                {
                    modelMatricies.Add(model.Transform.Matrix);

                    var mat = (DeferredRenderingGeoMaterial)model.Material;
                    materials.Add(new GPUDeferredGeoMaterial(mat.AlbedoColourTint, mat.IlluminationColor, mat.TextureRepeat, mat.TextureOffset, 1, mat.HasWorldpaceUVs));

                    var command = model.VAO.description;
                    drawCommands.Add(new DrawElementsIndirectData(
                        command.NumIndexes,
                        command.FirstIndex / sizeof(ushort),
                        command.BaseVertex,
                        command.NumInstances,
                        i++
                    ));
                }
            }

            drawIndirectBuffer.Update(drawCommands.ToArray());
            matriciesBuffer.Update(modelMatricies.ToArray());
            materialsBuffer.Update(materials.ToArray());
        }

        public virtual void RenderOccluderGeometry()
        {
            if (OccluderDrawIndirectBuffer == null)
                return;

            using var debugGroup = new DebugGroup(Name);
            OccluderDrawIndirectBuffer.Bind();
            OccluderMatriciesBuffer.BindRange(0, 1);
            OccluderMaterialsBuffer.BindRange(0, 2);

            MultiDrawIndirect(OccluderBatches, OccluderMatriciesBuffer, OccluderMaterialsBuffer);
        }

        public virtual void RenderNonOccluderGeometry()
        {
            if (NonOccluderDrawIndirectBuffer == null)
                return;

            using var debugGroup = new DebugGroup(Name);
            NonOccluderDrawIndirectBuffer.Bind();
            NonOccluderMatriciesBuffer.BindRange(0, 1);
            NonOccluderMaterialsBuffer.BindRange(0, 2);

            MultiDrawIndirect(NonOccluderBatches, NonOccluderMatriciesBuffer, NonOccluderMaterialsBuffer);
        }

        public void RenderLights(
            FrustumMaterial frustumMaterial,
            Shader SpotLightShader,
            Shader PointLightShader,
            SingleColorMaterial singleColorMaterial,
            Texture2D[] gbuffer,
            bool debugLights)
        {
            using var debugGroup = new DebugGroup(Name);

            if (PointLights.Any())
            {
                using var lightsDebugGroup = new DebugGroup("Point Lights");
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
                        var modelMatrix = MathFunctions.CreateModelMatrix(light.Position, Quaternion.Identity, new Vector3(light.Radius * 2));
                        singleColorMaterial.ModelMatrix = modelMatrix;
                        singleColorMaterial.Commit();
                        Primitives.Sphere.Draw(PrimitiveType.Lines);
                    }
                }
            }

            if (SpotLights.Any())
            {
                using var lightsDebugGroup = new DebugGroup("Spot Lights");
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

        private void MultiDrawIndirect(
            IEnumerable<RenderBatch> batches,
            Buffer<Matrix4> matriciesBuffer,
            Buffer<GPUDeferredGeoMaterial> materialsBuffer)
        {
            var drawCommandPtr = (IntPtr)0;
            var modelMatrixPtr = 0;
            var materialPtr = 0;
            var commandSize = Marshal.SizeOf<DrawElementsIndirectData>();
            var matrixSize = Marshal.SizeOf<Matrix4>();
            var materialSize = Marshal.SizeOf<GPUDeferredGeoMaterial>();
            foreach (var batch in batches)
            {
                var batchSize = batch.Models.Count;

                var oldShader = Shader.Current;
                batch.BindState();

                matriciesBuffer.BindRange(modelMatrixPtr, 1, batchSize * matrixSize);
                materialsBuffer.BindRange(materialPtr, 2, batchSize * materialSize);
                GL.MultiDrawElementsIndirect(
                    PrimitiveType.Triangles,
                    DrawElementsType.UnsignedShort,
                    drawCommandPtr,
                    batchSize,
                    0
                );

                drawCommandPtr += batchSize * commandSize;
                modelMatrixPtr += batchSize * matrixSize;
                materialPtr += batchSize * materialSize;
            }
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
                && mat1.NormalTexture == mat2.NormalTexture;
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