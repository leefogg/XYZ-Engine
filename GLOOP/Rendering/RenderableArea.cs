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
        private const int MaxLights = 200;

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

        public string Name { get; private set; }

        public List<Model> Models = new List<Model>();
        public List<PointLight> PointLights = new List<PointLight>();
        public List<SpotLight> SpotLights = new List<SpotLight>();
        public List<RenderBatch> OccluderBatches, NonOccluderBatches;

        private Buffer<GPUPointLight> PointLightsBuffer;
        private Buffer<GPUSpotLight> SpotLightsBuffer;
        private List<int> CulledSpotLights = new List<int>();
        private List<int> CulledPointLights = new List<int>();
        public IReadOnlyList<Model> Occluders { get; private set; }
        public IReadOnlyList<Model> NonOccluders { get; private set; }

        protected RenderableArea(string name)
        {
            Name = name;
        }

        public void Prepare()
        {
            PrepareLightBuffers();
        }

        public void UpdateLightBuffers()
        {
            FillPointLightsUBO();
            FillSpotLightsUBO();
        }

        public void UpdateModelBatches()
        {
            Occluders     =  Models.Where(o =>  o.IsOccluder && Camera.Current.IntersectsFrustumFast(o.BoundingBox)).ToList();
            NonOccluders  =  Models.Where(o => !o.IsOccluder && Camera.Current.IntersectsFrustumFast(o.BoundingBox)).ToList();
        }

        private void PrepareLightBuffers()
        {
            if (PointLights.Any())
                PointLightsBuffer = new Buffer<GPUPointLight>(
                    Math.Min(MaxLights, PointLights.Count),
                    BufferTarget.UniformBuffer,
                    BufferUsageHint.StreamDraw,
                    Name + " PointLights"
                );

            if (SpotLights.Any())
                SpotLightsBuffer = new Buffer<GPUSpotLight>(
                    Math.Min(MaxLights, SpotLights.Count),
                    BufferTarget.UniformBuffer,
                    BufferUsageHint.StreamDraw,
                    Name + " SpotLights"
                );
        }

        private void FillPointLightsUBO()
        {
            if (!PointLights.Any())
                return;

            CulledPointLights.Clear();

            var numCulledPointLights = 0;
            var lights = new GPUPointLight[Math.Min(MaxLights, PointLights.Count)];
            for (var i = 0; i < lights.Length; i++)
            {
                var light = PointLights[i];
                if (Camera.Current.IsInsideFrustum(light.Position, light.Radius))
                {
                    light.GetLightingScalars(out var diffuseScalar, out var specularScalar);
                    CulledPointLights.Add(i);
                    lights[numCulledPointLights++] = new GPUPointLight(
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

            CulledSpotLights.Clear();

            var numCulledSpotLights = 0;
            var lights = new GPUSpotLight[Math.Min(MaxLights, SpotLights.Count)];
            for (var i = 0; i < lights.Length; i++)
            {
                var light = SpotLights[i];
                if (Camera.Current.IsInsideFrustum(light.Position, light.Radius))
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

                    CulledSpotLights.Add(i);
                    lights[numCulledSpotLights++] = new GPUSpotLight(
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

        public void RenderLights(
            FrustumMaterial frustumMaterial,
            Shader SpotLightShader,
            Shader PointLightShader,
            SingleColorMaterial singleColorMaterial,
            Texture2D[] gbuffer,
            bool debugLights)
        {
            using var debugGroup = new DebugGroup(Name);

            var numCulledPointLights = CulledPointLights.Count;
            if (numCulledPointLights > 0)
            {
                using var lightsDebugGroup = new DebugGroup("Point Lights");
                PointLightsBuffer.Bind(1, 0);

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
                Primitives.Sphere.Draw(numInstances: numCulledPointLights);
                Metrics.LightsDrawn += numCulledPointLights;

                // Debug light spheres
                if (debugLights)
                {
                    foreach (var lightIdx in CulledPointLights)
                    {
                        var light = PointLights[lightIdx];
                        var modelMatrix = MathFunctions.CreateModelMatrix(light.Position, Quaternion.Identity, new Vector3(light.Radius * 2));
                        singleColorMaterial.ModelMatrix = modelMatrix;
                        singleColorMaterial.Commit();
                        Primitives.Sphere.Draw(PrimitiveType.Lines);
                    }
                }
            }

            var numCulledSpotLights = CulledSpotLights.Count;
            if (numCulledSpotLights > 0)
            {
                using var lightsDebugGroup = new DebugGroup("Spot Lights");
                SpotLightsBuffer.Bind(1, 0);

                var shader = SpotLightShader;
                shader.Use();
                Texture.Use(gbuffer, TextureUnit.Texture0);
                shader.Set("diffuseTex", TextureUnit.Texture0);
                shader.Set("positionTex", TextureUnit.Texture1);
                shader.Set("normalTex", TextureUnit.Texture2);
                shader.Set("specularTex", TextureUnit.Texture3);
                shader.Set("camPos", Camera.Current.Position);

                //Console.WriteLine(((float)culledSpotLights.Count / (float)scene.SpotLights.Count) * 100 + "% of spot lights");
                Primitives.Frustum.Draw(numInstances: numCulledSpotLights);
                Metrics.LightsDrawn += numCulledSpotLights;

                if (debugLights)
                {
                    var material = frustumMaterial;
                    foreach (var lightIdx in CulledSpotLights)
                    {
                        var light = SpotLights[lightIdx];
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
    }
}
