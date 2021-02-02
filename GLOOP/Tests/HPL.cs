﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using Assimp;
using GLOOP.Extensions;
using GLOOP.Rendering;
using GLOOP.SOMA;
using Newtonsoft.Json;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TextureWrapMode = OpenTK.Graphics.OpenGL4.TextureWrapMode;

namespace GLOOP.Tests
{
    public class HPL : Window {
        private Camera Camera;
        private Map map;

        private FrameBuffer GBuffers;
        private FrameBuffer LightingBuffer;
        private FrameBuffer[] BloomBuffers;
        private FrameBuffer StagingBuffer;
        private Shader PointLightShader;
        private Shader SpotLightShader;
        private SingleColorMaterial singleColorMaterial;
        private Shader FinalCombineShader;
        private Shader FullBrightShader;
        private Shader VerticalBlurShader;
        private Shader HorizontalBlurShader;
        private Shader BloomCombineShader;
        private Shader FXAAShader;
        private Shader SSAOShader;
        private bool DebugLights;

        private int debugGBufferTexture = -1;
        private bool debugLightBuffer;
        private bool useFXAA = false;
        private bool useSSAO = false;
        private int cameraUBO, pointLightsUBO, spotLightsUBO;
        const int maxLights = 500;

        public HPL(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings) {
            Camera = new DebugCamera(new Vector3(-17.039896f, 14.750014f, 64.48185f), new Vector3(), 90);
        }

        protected override void OnLoad() {
            base.OnLoad();

            GL.Enable(EnableCap.Blend);

            var sampler = GL.GenSampler();
            GL.SamplerParameter(sampler, SamplerParameterName.TextureWrapS, (float)TextureWrapMode.Repeat);
            GL.SamplerParameter(sampler, SamplerParameterName.TextureWrapT, (float)TextureWrapMode.Repeat);
            GL.SamplerParameter(sampler, SamplerParameterName.TextureWrapR, (float)TextureWrapMode.Repeat);
            GL.SamplerParameter(sampler, SamplerParameterName.TextureMinFilter, (float)TextureMinFilter.Linear);
            GL.SamplerParameter(sampler, SamplerParameterName.TextureMagFilter, (float)TextureMinFilter.Linear);

            GBuffers = new FrameBuffer(
                Width, Height,
                new[] { 
                    PixelInternalFormat.Rgb,        // Diffuse
                    PixelInternalFormat.Rgb16f,     // Position
                    PixelInternalFormat.Rgb16f,     // Normal
                    PixelInternalFormat.Rgba,       // Specular
                    PixelInternalFormat.Rgb16f      // Illumnination
                },
                true,
                "GBuffers"
            );
            LightingBuffer = new FrameBuffer(Width, Height, false, PixelInternalFormat.Rgb16f, 1, "Lighting");
            BloomBuffers = new FrameBuffer[]
            {
                new FrameBuffer(Width / 2, Height / 2, false, PixelInternalFormat.Rgb, name: "Vertical blur pass @ (1/2)"),
                new FrameBuffer(Width / 2, Height / 2, false, PixelInternalFormat.Rgb, name: "Horizonal blur pass @ (1/2)"),
                new FrameBuffer(Width / 4, Height / 4, false, PixelInternalFormat.Rgb, name: "Vertical blur pass @ (1/4)"),
                new FrameBuffer(Width / 4, Height / 4, false, PixelInternalFormat.Rgb, name: "Horizonal blur pass @ (1/4)"),
                new FrameBuffer(Width / 8, Height / 8, false, PixelInternalFormat.Rgb, name: "Vertical blur pass @ (1/8)"),
                new FrameBuffer(Width / 8, Height / 8, false, PixelInternalFormat.Rgb, name: "Horizonal blur pass @ (1/8)"),
            };
            StagingBuffer = new FrameBuffer(Width, Height, false, PixelInternalFormat.Rgb16f);

            var deferredGeoShader = new DeferredRenderingGeoShader();
            var deferredMaterial = new DeferredRenderingGeoMaterial(deferredGeoShader);
            PointLightShader = new DynamicPixelShader(
                "assets/shaders/deferred/LightPass/SingleLightVertexShader.vert",
                "assets/shaders/deferred/LightPass/SingleLightFragmentShader.frag",
                new Dictionary<string, string>
                {
                    { "LIGHTTYPE", "0" }
                },
                "Deferred Point light"
            );
            SpotLightShader = new DynamicPixelShader(
                "assets/shaders/deferred/LightPass/SingleLightVertexShader.vert",
                "assets/shaders/deferred/LightPass/SingleLightFragmentShader.frag",
                new Dictionary<string, string>
                {
                    { "LIGHTTYPE", "1" }
                },
                "Deferred Spot light"
            );
            singleColorMaterial = new SingleColorMaterial(Shader.SingleColorShader);
            singleColorMaterial.Color = new Vector4(0.25f);

            FinalCombineShader = new StaticPixelShader(
                "assets/shaders/FinalCombine/vertex.vert",
                "assets/shaders/FinalCombine/fragment.frag",
                name: "Final combine"
            );
            FullBrightShader = new StaticPixelShader(
                "assets/shaders/FullBright/2D/VertexShader.vert",
                "assets/shaders/FullBright/2D/FragmentShader.frag",
                name: "Fullbright"
            );
            VerticalBlurShader = new StaticPixelShader(
               "assets/shaders/Blur/VertexShader.vert",
               "assets/shaders/Blur/FragmentShader.frag",
               new Dictionary<string, string>
               {
                   { "Direction", "vec2(1.0,0.0)" }
               },
               "Blur Vertically"
            );
            HorizontalBlurShader = new StaticPixelShader(
               "assets/shaders/Blur/VertexShader.vert",
               "assets/shaders/Blur/FragmentShader.frag",
               new Dictionary<string, string>
               {
                   { "Direction", "vec2(0.0,1.0)" }
               },
               "Blur Horizontally"
            );
            BloomCombineShader = new DynamicPixelShader(
               "assets/shaders/Bloom/Combine/vertex.vert",
               "assets/shaders/Bloom/Combine/fragment.frag",
               name: "Bloom combine"
            );
            FXAAShader = new StaticPixelShader(
                "assets/shaders/FXAA/VertexShader.vert",
                "assets/shaders/FXAA/FragmentShader.frag",
                name: "FXAA"
            );
            SSAOShader = new StaticPixelShader(
                "assets/shaders/SSAO/VertexShader.vert",
                "assets/shaders/SSAO/FragmentShader.frag",
                name: "SSAO"
            );

            /*
            GL.GetActiveUniformBlock(PointLightShader.Handle, 1, ActiveUniformBlockParameter.UniformBlockDataSize, out int pointLightBufferSize); // 64
            GL.GetActiveUniformBlock(SpotLightShader.Handle, 1, ActiveUniformBlockParameter.UniformBlockDataSize, out int spotLightBufferSize); // 80
            var pointLightStructSize = Marshal.SizeOf<PointLight>();
            var spotLightStructSize = Marshal.SizeOf<SpotLight>();
            */

            var lab = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter00\00_03_laboratory\00_03_laboratory.hpm";
            var bedroom = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter00\00_01_apartment\00_01_apartment.hpm";
            var theta_outside = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter02\02_04_theta_outside\02_04_theta_outside.hpm";
            var upsilon = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter01\01_02_upsilon_inside\01_02_upsilon_inside.hpm";
            var theta_inside = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter02\02_05_theta_inside\02_05_theta_inside.hpm";
            var tau = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter04\04_02_tau_inside\04_02_tau_inside.hpm";
            var phi = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter05\05_01_phi_inside\05_01_phi_inside.hpm";
            var custom = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\custom\custom.hpm";
            var mapToLoad = custom;
            var metaFilePath = Path.Combine("meta", Path.GetFileName(mapToLoad));

            /*
            if (File.Exists(metaFilePath))
            {
                var textures = JsonConvert.DeserializeObject<TextureArrayManager.TextureShapeSummary[]>(File.ReadAllText(metaFilePath));
                foreach (var tex in textures)
                {
                    var toCreateRemaining = tex.AllocatedSlices;
                    var toCreate = (ushort)Math.Min(ushort.MaxValue, toCreateRemaining);
                    while (toCreateRemaining > 0)
                    {
                        TextureArrayManager.CreateTexture(tex.Shape, toCreate);
                        toCreateRemaining -= toCreate;
                    }
                }
            }
            */

            var assimp = new AssimpContext();
            var beforeMapLoad = DateTime.Now;
            map = new Map(
                mapToLoad,
                assimp,
                deferredMaterial
            );
            Console.WriteLine($"Time taken to load map {(DateTime.Now - beforeMapLoad).TotalSeconds} seconds");
            Console.WriteLine($"Time taken to load textures {Metrics.TimeLoadingTextures.TotalSeconds} seconds");
            Console.WriteLine("Number of textures: " + Metrics.TextureCount);
            Console.WriteLine("Textures: " + Metrics.TexturesBytesUsed / 1024 / 1024 + "MB");
            Console.WriteLine("Models vertcies: " + Metrics.ModelsBytesUsed / 1024 / 1024 + "MB");
            Console.WriteLine("Models indicies: " + Metrics.ModelsIndiciesBytesUsed/ 1024 / 1024 + "MB");

            setupUniformBuffers();

            var usedTextures = TextureArrayManager.GetSummary();
            foreach (var alloc in usedTextures)
                Console.WriteLine($"Shape {alloc.Shape} used {alloc.AllocatedSlices} slices");
            Directory.CreateDirectory("meta");
            var summaryJson = JsonConvert.SerializeObject(usedTextures);
            File.WriteAllText(metaFilePath, summaryJson);
        }

        private void setupUniformBuffers()
        {
            setupCameraUniformBuffer();
            setupLightingUBO();
            updatePointLightsUBO();
            updateSpotLightsUBO();
        }

        private void setupCameraUniformBuffer()
        {
            cameraUBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.UniformBuffer, cameraUBO);
            var size = new Vector4[8].SizeInBytes();
            GL.NamedBufferData(cameraUBO, size, (IntPtr)0, BufferUsageHint.StreamRead);
            GL.BindBufferRange(BufferRangeTarget.UniformBuffer, 0, cameraUBO, (IntPtr)0, size);
        }

        private void setupLightingUBO()
        {
            {
                pointLightsUBO = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.UniformBuffer, pointLightsUBO);
                GL.ObjectLabel(ObjectLabelIdentifier.Buffer, pointLightsUBO, 11, "PointLights");
                var data = new PointLight[Math.Min(maxLights, map.PointLights.Count)];
                var size = data.SizeInBytes();
                GL.NamedBufferData(pointLightsUBO, size, (IntPtr)0, BufferUsageHint.StaticDraw);
                GL.BindBufferRange(BufferRangeTarget.UniformBuffer, 1, pointLightsUBO, (IntPtr)0, size);
            }
            {
                spotLightsUBO = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.UniformBuffer, spotLightsUBO);
                GL.ObjectLabel(ObjectLabelIdentifier.Buffer, spotLightsUBO, 10, "SpotLights");
                var data = new SpotLight[Math.Min(maxLights, map.SpotLights.Count)];
                var size = data.SizeInBytes();
                GL.NamedBufferData(spotLightsUBO, size, (IntPtr)0, BufferUsageHint.StaticDraw);
                GL.BindBufferRange(BufferRangeTarget.UniformBuffer, 2, spotLightsUBO, (IntPtr)0, size);
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GBuffers.Use();
            var clearColor = DebugLights ? 0.05f : 0;
            GL.ClearColor(clearColor, clearColor, clearColor, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var projectionMatrix = Camera.ProjectionMatrix;
            var viewMatrix = Camera.ViewMatrix;
            updateCameraUBO();

            map?.Render(projectionMatrix, viewMatrix);

            FinishDeferredRendering(projectionMatrix, viewMatrix);

            SwapBuffers();
            FrameNumber++;
        }

        private void updateCameraUBO()
        {
            var projectionMatrix = Camera.ProjectionMatrix;
            var viewMatrix = Camera.ViewMatrix;
            var data = new Vector4[]
            {
                projectionMatrix.Row0,
                projectionMatrix.Row1,
                projectionMatrix.Row2,
                projectionMatrix.Row3,
                viewMatrix.Row0,
                viewMatrix.Row1,
                viewMatrix.Row2,
                viewMatrix.Row3,
            };
            GL.NamedBufferData(cameraUBO, data.SizeInBytes(), data, BufferUsageHint.StreamRead);
        }

        private void updatePointLightsUBO()
        {
            var data = new PointLight[Math.Min(maxLights, map.PointLights.Count)];
            for (var i = 0; i < data.Length; i++)
            {
                var pointLight = map.PointLights[i];
                pointLight.GetLightingScalars(out float diffuseScalar, out float specularScalar);
                data[i] = new PointLight(
                    pointLight.Position,
                    pointLight.Color,
                    pointLight.Brightness,
                    pointLight.Radius,
                    pointLight.FalloffPower,
                    diffuseScalar,
                    specularScalar
                );
            }
            GL.NamedBufferSubData(pointLightsUBO, (IntPtr)0, data.SizeInBytes(), data);
        }

        private void updateSpotLightsUBO()
        {
            var data = new SpotLight[Math.Min(maxLights, map.SpotLights.Count)];
            for (var i = 0; i < data.Length; i++)
            {
                var spotLight = map.SpotLights[i];
                spotLight.GetLightingScalars(out float diffuseScalar, out float specularScalar);
                var dir = spotLight.Rotation * new Vector3(0, 0, 1);
                data[i] = new SpotLight(
                    spotLight.Position,
                    spotLight.Color,
                    dir,
                    spotLight.Brightness,
                    spotLight.Radius,
                    spotLight.FalloffPower,
                    spotLight.AngularFalloffPower,
                    spotLight.FOV,
                    diffuseScalar,
                    specularScalar
                );
            }

            GL.NamedBufferSubData(spotLightsUBO, (IntPtr)0, data.SizeInBytes(), data);
        }

        private void FinishDeferredRendering(Matrix4 projectionMatrix, Matrix4 viewMatrix)
        {
            GL.Disable(EnableCap.DepthTest);
            //GL.DepthMask(false);

            if (debugGBufferTexture > -1)
            {
                DisplayGBuffer(debugGBufferTexture);
            }
            else
            {
                DoLightPass(projectionMatrix, viewMatrix, new Vector3(0.05f));

                if (debugLightBuffer) {
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                    GL.ClearColor(0, 0, 0, 1);
                    GL.Clear(ClearBufferMask.ColorBufferBit);

                    var shader = FullBrightShader;
                    shader.Use();
                    LightingBuffer.ColorBuffers[0].Use(TextureUnit.Texture0);
                    shader.Set("texture0", TextureUnit.Texture0);
                    Primitives.Quad.Draw();
                } else {
                    StagingBuffer.Use();
                    GL.ClearColor(0, 0, 0, 1);
                    GL.Clear(ClearBufferMask.ColorBufferBit);

                    Shader shader = FinalCombineShader;
                    shader.Use();
                    LightingBuffer.ColorBuffers[0].Use(TextureUnit.Texture0);
                    GBuffers.ColorBuffers[0].Use(TextureUnit.Texture1);
                    shader.Set("texture0", TextureUnit.Texture0);
                    shader.Set("texture1", TextureUnit.Texture1);
                    Primitives.Quad.Draw();

                    DoBloomPass();

                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                    shader = useFXAA ? FXAAShader : FullBrightShader;
                    shader.Use();
                    StagingBuffer.ColorBuffers[0].Use(TextureUnit.Texture0);
                    shader.Set("Texture", TextureUnit.Texture0);
                    Primitives.Quad.Draw();
                }
            }

            //GL.DepthMask(true);
            GL.Enable(EnableCap.DepthTest);
        }

        private void DoBloomPass()
        {
            var offsets = new[] { 0.00000f, 0.00289f, 0.00675f, 0.01060f, 0.01446f, 0.01832f, 0.02217f, 0.02603f, 0.02988f, 0.03374f, 0.03760f, 0.04145f, 0.04531f, 0.04917f, 0.05302f, 0.05688f, 0.06074f, 0.06460f, 0.06846f, 0.07231f, 0.07617f, 0.08003f, 0.08389f, 0.08775f };
            var weights = new[] { 1.00000f, 1.94883f, 1.75699f, 1.45802f, 1.11366f, 0.78296f, 0.50667f, 0.30179f, 0.16545f, 0.08349f, 0.03878f, 0.01658f, 0.00652f, 0.00236f, 0.00079f, 0.00024f, 0.00007f, 0.00002f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f };

            var shader = HorizontalBlurShader;
            shader.Use();
            shader.Set("diffuseMap", TextureUnit.Texture0);
            for (var i = 0; i < offsets.Length; i++)
            {
                shader.Set($"offsets[{i}]", offsets[i]);
                shader.Set($"weights[{i}]", weights[i]);
            }
            shader = VerticalBlurShader;
            shader.Use();
            shader.Set("diffuseMap", TextureUnit.Texture0);
            for (var i = 0; i < offsets.Length; i++)
            {
                shader.Set($"offsets[{i}]", offsets[i]);
                shader.Set($"weights[{i}]", weights[i]);
            }

            int width = Width,
                height = Height;
            var previousTexture = GBuffers.ColorBuffers[(int)GBufferTexture.Illumination];
            for (var i = 0; i < BloomBuffers.Length; i++)
            {
                width /= 2;
                height /= 2;
                GL.Viewport(0, 0, width, height);

                VerticalBlurShader.Use();
                BloomBuffers[i].Use();
                previousTexture.Use(TextureUnit.Texture0);
                Primitives.Quad.Draw();
                previousTexture = BloomBuffers[i].ColorBuffers[0];

                i++;
                HorizontalBlurShader.Use();
                BloomBuffers[i].Use();
                previousTexture.Use(TextureUnit.Texture0);
                Primitives.Quad.Draw();
                previousTexture = BloomBuffers[i].ColorBuffers[0];
            }

            // Add all to finished frame
            GL.Viewport(0, 0, Width, Height);
            StagingBuffer.Use();
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);

            shader = BloomCombineShader;
            shader.Use();
            BloomBuffers[1].ColorBuffers[0].Use(TextureUnit.Texture0);
            BloomBuffers[3].ColorBuffers[0].Use(TextureUnit.Texture1);
            BloomBuffers[5].ColorBuffers[0].Use(TextureUnit.Texture2);
            shader.Set("blurMap0", TextureUnit.Texture0);
            shader.Set("blurMap1", TextureUnit.Texture1);
            shader.Set("blurMap2", TextureUnit.Texture2);
            Primitives.Quad.Draw();

            GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
        }

        private void DisplayGBuffer(int buffer)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            var shader = FullBrightShader;
            shader.Use();
            GBuffers.ColorBuffers[buffer].Use(TextureUnit.Texture0);
            shader.Set("texture0", 0);
            Primitives.Quad.Draw();
        }

        public void DoLightPass(Matrix4 projectionMatrix, Matrix4 viewMatrix, Vector3 ambientColor)
        {
            LightingBuffer.Use();
            GL.ClearColor(ambientColor.X, ambientColor.Y, ambientColor.Z, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
            GL.CullFace(CullFaceMode.Front);

            if (map.PointLights.Any()) {
                var shader = PointLightShader;
                shader.Use();
                GBuffers.ColorBuffers[(int)GBufferTexture.Diffuse].Use(TextureUnit.Texture0);
                GBuffers.ColorBuffers[(int)GBufferTexture.Position].Use(TextureUnit.Texture1);
                GBuffers.ColorBuffers[(int)GBufferTexture.Normal].Use(TextureUnit.Texture2);
                GBuffers.ColorBuffers[(int)GBufferTexture.Specular].Use(TextureUnit.Texture3);
                shader.Set("diffuseTex", TextureUnit.Texture0);
                shader.Set("positionTex", TextureUnit.Texture1);
                shader.Set("normalTex", TextureUnit.Texture2);
                shader.Set("specularTex", TextureUnit.Texture3);
                shader.Set("camPos", Camera.Position);
                //TODO: Could render a 2D circle in screenspace instead of a sphere

                var numPointLights = Math.Min(maxLights, map.PointLights.Count);
                
                Primitives.Sphere.Draw(numInstances: numPointLights);

                // Debug light spheres
                if (DebugLights)
                {
                    for (var i = 0; i < map.PointLights.Count; i++)
                    {
                        var light = map.PointLights[i];
                        var modelMatrix = MathFunctions.CreateModelMatrix(light.Position, new OpenTK.Mathematics.Quaternion(), new Vector3(light.Radius * 2));
                        singleColorMaterial.ProjectionMatrix = projectionMatrix;
                        singleColorMaterial.ViewMatrix = viewMatrix;
                        singleColorMaterial.ModelMatrix = modelMatrix;
                        singleColorMaterial.Commit();
                        Primitives.Sphere.Draw(OpenTK.Graphics.OpenGL4.PrimitiveType.Lines);
                    }
                }
            }

            if (map.SpotLights.Any()) {
                var numSpotLights = Math.Min(maxLights, map.SpotLights.Count);

                Shader shader = SpotLightShader;
                shader.Use();
                GBuffers.ColorBuffers[(int)GBufferTexture.Diffuse].Use(TextureUnit.Texture0);
                GBuffers.ColorBuffers[(int)GBufferTexture.Position].Use(TextureUnit.Texture1);
                GBuffers.ColorBuffers[(int)GBufferTexture.Normal].Use(TextureUnit.Texture2);
                GBuffers.ColorBuffers[(int)GBufferTexture.Specular].Use(TextureUnit.Texture3);
                shader.Set("diffuseTex", TextureUnit.Texture0);
                shader.Set("positionTex", TextureUnit.Texture1);
                shader.Set("normalTex", TextureUnit.Texture2);
                shader.Set("specularTex", TextureUnit.Texture3);
                shader.Set("camPos", Camera.Position);

                Primitives.Sphere.Draw(numInstances: numSpotLights);

                if (DebugLights)
                {
                    for (var i = 0; i < numSpotLights; i++)
                    {
                        var light = map.SpotLights[i];
                        var modelMatrix = MathFunctions.CreateModelMatrix(light.Position, new OpenTK.Mathematics.Quaternion(), new Vector3(light.Radius * 2));
                        singleColorMaterial.ModelMatrix = modelMatrix;
                        singleColorMaterial.ProjectionMatrix = projectionMatrix;
                        singleColorMaterial.ViewMatrix = viewMatrix;
                        singleColorMaterial.Commit();
                        Primitives.Sphere.Draw(OpenTK.Graphics.OpenGL4.PrimitiveType.Lines);
                    }
                }
            }

            GL.CullFace(CullFaceMode.Back);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
            GL.BlendEquation(BlendEquationMode.FuncReverseSubtract);

            if (useSSAO)
            {
                var shader = SSAOShader;
                shader.Use();
                GBuffers.ColorBuffers[(int)GBufferTexture.Position].Use(TextureUnit.Texture0);
                GBuffers.ColorBuffers[(int)GBufferTexture.Normal].Use(TextureUnit.Texture1);
                shader.Set("positionTexture", TextureUnit.Texture0);
                shader.Set("normalTexture", TextureUnit.Texture1);
                var cameraRotation = Camera.Rotation * (float)(Math.PI / 180);
                var rotationMatrix = 
                    Matrix4.CreateRotationX(cameraRotation.X) * 
                    Matrix4.CreateRotationY(cameraRotation.Y) * 
                    Matrix4.CreateRotationZ(cameraRotation.Z);
                rotationMatrix.Invert();
                shader.Set("RotationMatrix", rotationMatrix);
                //shader.Set("Time", FrameNumber / 0.0001f);
                shader.Set("campos", Camera.Position);

                Primitives.Quad.Draw();
            }

            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            Camera.Update(KeyboardState);

            if (IsFocused) {
                var input = KeyboardState;

                if (KeyboardState.IsKeyPressed(Keys.L))
                    DebugLights = !DebugLights;

                if (input.IsKeyDown(Keys.X))
                    SOMAModel.Offset.X += 0.01f;
                if (input.IsKeyDown(Keys.Y))
                    SOMAModel.Offset.Y += 0.01f;
                if (input.IsKeyDown(Keys.Z))
                    SOMAModel.Offset.Z += 0.01f;

                if (input.IsKeyPressed(Keys.D0))
                    debugGBufferTexture = -1;
                if (input.IsKeyReleased(Keys.D1))
                    debugGBufferTexture = (int)GBufferTexture.Diffuse;
                if (input.IsKeyReleased(Keys.D2))
                    debugGBufferTexture = (int)GBufferTexture.Position;
                if (input.IsKeyReleased(Keys.D3))
                    debugGBufferTexture = (int)GBufferTexture.Normal;
                if (input.IsKeyReleased(Keys.D4))
                    debugGBufferTexture = (int)GBufferTexture.Specular;
                if (input.IsKeyReleased(Keys.D5))
                    debugGBufferTexture = (int)GBufferTexture.Illumination;
                if (input.IsKeyReleased(Keys.D9))
                    debugLightBuffer = !debugLightBuffer;
                if (input.IsKeyReleased(Keys.F))
                    useFXAA = !useFXAA;
                if (input.IsKeyReleased(Keys.O))
                    useSSAO = !useSSAO;
                if (input.IsKeyPressed(Keys.V))
                    VSync = VSync == VSyncMode.Off ? VSyncMode.On : VSyncMode.Off; 
            }
        }

        protected override void OnClosing(CancelEventArgs e) {
            Mouse.Grabbed = false;

            base.OnClosing(e);
        }


        [StructLayout(LayoutKind.Explicit, Size = 64)]
        private struct PointLight
        {
            [FieldOffset(0)] Vector3 position;
            [FieldOffset(16)] Vector3 color;
            [FieldOffset(32)] float brightness;
            [FieldOffset(36)] float radius;
            [FieldOffset(40)] float falloffPow;
            [FieldOffset(44)] float diffuseScalar;
            [FieldOffset(58)] float specularScalar;

            public PointLight(
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

        [StructLayout(LayoutKind.Explicit, Size = 80)]
        private struct SpotLight
        {
            [FieldOffset(0)] Vector3 position;
            [FieldOffset(16)] Vector3 color;
            [FieldOffset(32)] Vector3 direction;
            [FieldOffset(48)] float brightness;
            [FieldOffset(52)] float radius;
            [FieldOffset(56)] float falloffPow;
            [FieldOffset(60)] float angularFalloffPow;
            [FieldOffset(64)] float FOV;
            [FieldOffset(68)] float diffuseScalar;
            [FieldOffset(72)] float specularScalar;

            public SpotLight(
                Vector3 position,
                Vector3 color,
                Vector3 direction,
                float brightness,
                float radius,
                float falloffPow,
                float angularFalloffPow,
                float fov,
                float diffuseScalar,
                float specularScalar
            )
            {
                this.position = position;
                this.color = color;
                this.direction = direction;
                this.brightness = brightness;
                this.radius = radius;
                this.falloffPow = falloffPow;
                this.angularFalloffPow = angularFalloffPow;
                this.FOV = fov;
                this.diffuseScalar = diffuseScalar;
                this.specularScalar = specularScalar;
            }
        }
        public enum GBufferTexture
        {
            Diffuse,
            Position,
            Normal,
            Specular,
            Illumination
        }
    }
}