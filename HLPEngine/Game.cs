using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using Assimp;
using GLOOP.Extensions;
using GLOOP.Rendering;
using GLOOP.HPL;
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
using GLOOP.Rendering.Materials;
using GLOOP.Tests.Assets.Shaders;
using GLOOP.HPL.Loading;
using HLPEngine;
using Valve.VR;
using System.Diagnostics;
using GLOOP.Rendering.Debugging;

namespace GLOOP.HPL
{
    public class Game : Window {
        private Camera Camera;
        private Scene scene;

        private FrameBuffer GBuffers;
        private FrameBuffer LightingBuffer;
        private FrameBuffer[] BloomBuffers;
#if VR
        private FrameBuffer LeftEyeBuffer;
        private FrameBuffer RightEyeBuffer;
#else
        private FrameBuffer FinalBuffer;
#endif
        private Shader PointLightShader;
        private Shader SpotLightShader;
        private Texture2D NoiseMap;
        private SingleColorMaterial singleColorMaterial;
        private Shader FinalCombineShader;
        private Shader FullBrightShader;
        private Shader ExtractBright;
        private Shader VerticalBlurShader;
        private Shader HorizontalBlurShader;
        private Shader BloomCombineShader;
        private Shader FXAAShader;
        private Shader SSAOShader;
        private Shader ColorCorrectionShader;
        private FrustumMaterial frustumMaterial;
        private QueryPool queryPool;
        private List<(VisibilityPortal, Query)> PortalQueries = new List<(VisibilityPortal, Query)>();

        private bool debugLights;
        private int debugGBufferTexture = -1;
        private bool debugLightBuffer;
        private bool useFXAA = false;
        private bool useSSAO = false;
        private bool enableBloom = false;
        private bool showBoundingBoxes = false;
        private const bool enablePortalCulling = true;

        private Query GeoPassQuery;
        private Buffer<float> bloomBuffer;
        private List<VisibilityArea> VisibleAreas = new List<VisibilityArea>();

        private int bloomDataStride = 1000;
        private float elapsedMilliseconds = 0;
        private readonly DateTime startTime = DateTime.Now;
        private readonly FrameBuffer backBuffer;
        private int frameBufferWidth, frameBufferHeight;

        private readonly Vector3
            CustomMapCameraPosition = new Vector3(6.3353596f, 1.6000088f, 8.1601305f),
            PhiMapCameraPosition = new Vector3(-17.039896f, 14.750014f, 64.48185f),
            deltaMapCameraPosition = new Vector3(0, 145, -10),
            thetaTunnelsMapCameraPosition = new Vector3(4, 9, -61),
            LightsMapCameraPosition = new Vector3(-0.5143715f, 4.3500123f, 11.639848f),
            PortalsMapCameraPosition = new Vector3(4.5954947f, 1.85f, 16.95526f),
            TauMapCameraPosition = new Vector3(26.263678f, 1.7000114f, 36.090767f),
            ThetaExitCameraPosition = new Vector3(11.340768f, 1.6000444f, 47.520298f),
            WauMapCameraPosition = new Vector3(-26.12f, 93.691f, 167.313f),
            AwakeMapCameraPosition = new Vector3(9.325157f, -0.44998702f, 50.61429f);

        public Game(int width, int height, GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings) {
            Camera = new DebugCamera(CustomMapCameraPosition, new Vector3(), 90)
            {
                Width = width,
                Height = height
            };
            Camera.Current = Camera;

            backBuffer = new FrameBuffer(0, Size.X, Size.Y);
            frameBufferWidth = width;
            frameBufferHeight = height;

#if VR
            DebugCamera.MAX_LOOK_UP = DebugCamera.MAX_LOOK_DOWN = 0;
#endif
        }

        protected override void OnLoad() {
            base.OnLoad();

            GL.Enable(EnableCap.Blend);

            var width = frameBufferWidth;
            var height = frameBufferHeight;

            GBuffers = new FrameBuffer(
                width, height,
                new[] { 
                    PixelInternalFormat.Srgb8Alpha8,// Diffuse
                    PixelInternalFormat.Rgb16f,     // Position
                    PixelInternalFormat.Rgb8,       // Normal
                    PixelInternalFormat.Srgb8Alpha8,// Specular
                },
                true,
                "GBuffers"
            );
            LightingBuffer = new FrameBuffer(width, height, false, PixelInternalFormat.Rgb16f, 1, "Lighting");
            BloomBuffers = new FrameBuffer[]
            {
                new FrameBuffer(width / 2, height / 2, false, PixelInternalFormat.Rgb, name: "Vertical blur pass @ (1/2)"),
                new FrameBuffer(width / 2, height / 2, false, PixelInternalFormat.Rgb, name: "Horizonal blur pass @ (1/2)"),
                new FrameBuffer(width / 4, height / 4, false, PixelInternalFormat.Rgb, name: "Vertical blur pass @ (1/4)"),
                new FrameBuffer(width / 4, height / 4, false, PixelInternalFormat.Rgb, name: "Horizonal blur pass @ (1/4)"),
                new FrameBuffer(width / 8, height / 8, false, PixelInternalFormat.Rgb, name: "Vertical blur pass @ (1/8)"),
                new FrameBuffer(width / 8, height / 8, false, PixelInternalFormat.Rgb, name: "Horizonal blur pass @ (1/8)"),
            };
#if VR
            LeftEyeBuffer = new FrameBuffer(width, height, false, PixelInternalFormat.Rgba, 1, "LeftEyeBuffer");
            RightEyeBuffer = new FrameBuffer(width, height, false, PixelInternalFormat.Rgba, 1, "RightEyeBuffer");
#else
            FinalBuffer = new FrameBuffer(width, height, false, PixelInternalFormat.Rgb, 1, "FinalBuffer");
#endif
            PostMan.Init(width, height, PixelInternalFormat.Rgb16f);

            var deferredMaterial = new DeferredRenderingGeoMaterial();
            PointLightShader = new DynamicPixelShader(
                "assets/shaders/deferred/LightPass/VertexShader.vert",
                "assets/shaders/deferred/LightPass/FragmentShader.frag",
                new Dictionary<string, string>
                {
                    { "LIGHTTYPE", "0" }
                },
                "Deferred Point light"
            );
            SpotLightShader = new DynamicPixelShader(
                "assets/shaders/deferred/LightPass/VertexShader.vert",
                "assets/shaders/deferred/LightPass/FragmentShader.frag",
                new Dictionary<string, string>
                {
                    { "LIGHTTYPE", "1" }
                },
                "Deferred Spot light"
            );
            singleColorMaterial = new SingleColorMaterial(Shader.SingleColorShader);
            singleColorMaterial.Color = new Vector4(1f);

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
            ExtractBright = new StaticPixelShader(
                "assets/shaders/Bloom/Extract/vertex.vert",
               "assets/shaders/Bloom/Extract/fragment.frag",
                name: "Extract brightness"
            );
            VerticalBlurShader = new StaticPixelShader(
               "assets/shaders/Blur/VertexShader.vert",
               "assets/shaders/Blur/FragmentShader.frag",
               new Dictionary<string, string>
               {
                   { "Direction", "vec2(0.0,1.0)" }
               },
               "Blur Vertically"
            );
            HorizontalBlurShader = new StaticPixelShader(
               "assets/shaders/Blur/VertexShader.vert",
               "assets/shaders/Blur/FragmentShader.frag",
               new Dictionary<string, string>
               {
                   { "Direction", "vec2(1.0,0.0)" }
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
            ColorCorrectionShader = new DynamicPixelShader(
                "assets/shaders/ColorCorrection/vertex.vert",
                "assets/shaders/ColorCorrection/fragment.frag",
                name: "Color Correction"
            );
            frustumMaterial = new FrustumMaterial(new FrustumShader());
            queryPool = new QueryPool(15);

            var lab = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter00\00_03_laboratory\00_03_laboratory.hpm";
            var bedroom = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter00\00_01_apartment\00_01_apartment.hpm";
            var awake = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter01\01_01_upsilon_awake\01_01_upsilon_awake.hpm";
            var theta_outside = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter02\02_04_theta_outside\02_04_theta_outside.hpm";
            var upsilon = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter01\01_02_upsilon_inside\01_02_upsilon_inside.hpm";
            var theta_inside = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter02\02_05_theta_inside\02_05_theta_inside.hpm";
            var theta_tunnels = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter02\02_06_theta_tunnels\02_06_theta_tunnels.hpm";
            var tau = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter04\04_02_tau_inside\04_02_tau_inside.hpm";
            var wau = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter04\04_03_tau_escape\04_03_tau_escape.hpm";
            var delta = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter02\02_03_delta\02_03_delta.hpm";
            var phi = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter05\05_01_phi_inside\05_01_phi_inside.hpm";
            var thetaExit = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter02\02_07_theta_exit\02_07_theta_exit.hpm";
            var custom = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\custom\custom.hpm";
            var boundingBoxes = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\Testing\BoundingBoxes\BoundingBoxes.hpm";
            var terrain = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\Testing\Terrain\Terrain.hpm";
            var lights = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\Testing\Lights\Lights.hpm";
            var portals = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\Testing\Portals\Portals.hpm";
            var Box3Contains = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\Testing\Box3Contains\Box3Contains.hpm";
            var mapToLoad = custom;

            /*
            var metaFilePath = Path.Combine("meta", Path.GetFileName(mapToLoad));
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
            var map = new Map(
                mapToLoad,
                assimp,
                deferredMaterial
            );
            scene = map.ToScene();
            var afterMapLoad = DateTime.Now;
            var beforeMapSort = DateTime.Now;
            scene.Prepare();
            foreach (var area in scene.VisibilityAreas.Values)
                area.Prepare();
            var afterMapSort = DateTime.Now;

            Console.WriteLine($"Time taken to sort map {(afterMapSort - beforeMapSort).TotalSeconds} seconds");
            Console.WriteLine($"Time taken to load map {(afterMapLoad - beforeMapLoad).TotalSeconds} seconds");
            Console.WriteLine($"Time taken to load models {Metrics.TimeLoadingModels.TotalSeconds} seconds");
            Console.WriteLine($"Time taken to load textures {Metrics.TimeLoadingTextures.TotalSeconds} seconds");
            Console.WriteLine("Number of textures: " + Metrics.TextureCount);
            Console.WriteLine("Textures: " + Metrics.TexturesBytesUsed / 1024 / 1024 + "MB");
            Console.WriteLine("Models vertcies: " + Metrics.ModelsVertciesBytesUsed / 1024 / 1024 + "MB");
            Console.WriteLine("Models indicies: " + Metrics.ModelsIndiciesBytesUsed/ 1024 / 1024 + "MB");
            var numStatic = map.Entities.Where(e => e.IsStatic).Sum(e => e.Models.Count);
            var numStaticOccluders = map.Entities.Where(e => e.IsStatic && e.IsOccluder).Sum(e => e.Models.Count);
            var numDynamic = map.Entities.Where(e => !e.IsStatic).Sum(e => e.Models.Count);
            var allModels = scene.Models.Count() + scene.VisibilityAreas.Values.SelectMany(area => area.Models).Count();
            Console.WriteLine($"Scene: {numStatic} Static, {numStaticOccluders} of which occlude. {numDynamic} Dynamic. {allModels} Total.");

            if (!enablePortalCulling)
                VisibleAreas.AddRange(scene.VisibilityAreas.Values);

            setupBuffers();

            /*
            var usedTextures = TextureArrayManager.GetSummary();
            foreach (var alloc in usedTextures)
                Console.WriteLine($"Shape {alloc.Shape} used {alloc.AllocatedSlices} slices");
            Directory.CreateDirectory("meta");
            var summaryJson = JsonConvert.SerializeObject(usedTextures);
            File.WriteAllText(metaFilePath, summaryJson);
            */
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            ReadbackQueries();

#if VR
            if (FrameNumber == 1)
                VRSystem.SetOriginHeadTransform();

            VRSystem.UpdateEyes();
            VRSystem.UpdatePoses();

            updateCameraUBO(
                VRSystem.GetEyeProjectionMatrix(EVREye.Eye_Left), 
                Camera.ViewMatrix * VRSystem.GetEyeViewMatrix(EVREye.Eye_Left)
            );
            ResetGBuffer();
            VRSystem.RenderEyeHiddenAreaMesh(EVREye.Eye_Left, FullBrightShader);
            RenderPass(LeftEyeBuffer);

            updateCameraUBO(
                VRSystem.GetEyeProjectionMatrix(EVREye.Eye_Right),
                Camera.ViewMatrix * VRSystem.GetEyeViewMatrix(EVREye.Eye_Right)
            );
            ResetGBuffer();
            VRSystem.RenderEyeHiddenAreaMesh(EVREye.Eye_Right, FullBrightShader);
            RenderPass(RightEyeBuffer);

            VRSystem.SubmitEye(LeftEyeBuffer.ColorBuffers[0], EVREye.Eye_Left);
            VRSystem.SubmitEye(RightEyeBuffer.ColorBuffers[0], EVREye.Eye_Right);

            LeftEyeBuffer.BlitTo(backBuffer, ClearBufferMask.ColorBufferBit);
#else
            updateCameraUBO(Camera.ProjectionMatrix, Camera.ViewMatrix);
            ResetGBuffer();
            RenderPass(FinalBuffer);
            FinalBuffer.BlitTo(backBuffer, ClearBufferMask.ColorBufferBit);
#endif

            SwapBuffers();
            NewFrame();
            elapsedMilliseconds = (float)(DateTime.Now - startTime).TotalMilliseconds;
            Title = FPS.ToString() + "FPS";
        }

        private void DetermineVisibleAreas()
        {
            if (!enablePortalCulling)
                return;

            // Dont need to check every frame
            // Also must only run every odd frame for VR support
            if ((FrameNumber & 1) != 0)
                return;

            VisibleAreas.Clear();

            // Get touching areas
            // This should be first to render closer areas first
            // "A visibility area that is connected to a portal will only be visible if the portal is visible or if the camera is inside it."
            VisibleAreas.AddRange(scene.VisibilityAreas.Values.Where(area => area.BoundingBox.Contains(Camera.Position)));

            // To avoid seams, always consider all connecting areas of touching portals to be visible
            // "If the camera is inside of a visibility area then all the portals connected to it will be used as a portal to the outside."
            var touchingPortals = scene.VisibilityPortals.Where(area => area.BoundingBox.Contains(Camera.Position));
            foreach (var portal in touchingPortals)
                foreach (var areaName in portal.VisibilityAreas)
                    VisibleAreas.Add(scene.VisibilityAreas[areaName]);

            if (PortalQueries.Any(pair => !pair.Item2.IsResultAvailable()))
                return;
            foreach (var (portal, query) in PortalQueries)
                if (query.GetResult() > 0) // Is visible
                    VisibleAreas.AddRange(portal.VisibilityAreas.Select(areaName => scene.VisibilityAreas[areaName]));
            PortalQueries.Clear();

            using (new DebugGroup("Portals"))
            {
                // Render portals
                GL.ColorMask(false, false, false, false);
                GL.Disable(EnableCap.CullFace);
                GL.DepthMask(false);
                // Dispatch queries for rooms visible from previous queries and current areas
                foreach (var portal in VisibleAreas.SelectMany(area => area.ConnectingPortals).Except(PortalQueries.Select(x => x.Item1)).Distinct())
                {
                    using var query = queryPool.BeginScope(QueryTarget.AnySamplesPassed);
                    Draw.Box(portal.ModelMatrix, new Vector4(1,1,0,0));
                    PortalQueries.Add((portal, query));
                }
                GL.ColorMask(true, true, true, true);
                GL.DepthMask(true);
                GL.Enable(EnableCap.CullFace);
            }
            
            VisibleAreas = VisibleAreas.Distinct().ToList(); // Remove duplicates

            // If we're flying around outside any area, just show them all
            if (!VisibleAreas.Any())
                VisibleAreas.AddRange(scene.VisibilityAreas.Values);
        }

        private void ResetGBuffer()
        {
            GBuffers.Use();
            var clearColor = debugLights ? 0.2f : 0;
            GL.ClearColor(clearColor, clearColor, clearColor, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        private void RenderPass(FrameBuffer finalBuffer)
        {
            var currentBuffer = PostMan.NextFramebuffer;

            GL.Viewport(0, 0, frameBufferWidth, frameBufferHeight);
            GBufferPass(currentBuffer);

            if (debugGBufferTexture > -1)
            {
                DisplayGBuffer(currentBuffer = PostMan.NextFramebuffer, debugGBufferTexture);
            }
            else
            {
                ResolveGBuffer(currentBuffer = PostMan.NextFramebuffer);
                if (!debugLightBuffer)
                {
                    if (enableBloom)
                        currentBuffer = DoBloomPass(currentBuffer.ColorBuffers[0]);

                    var newBuffer = PostMan.NextFramebuffer;
                    using (new DebugGroup("Color Correction"))
                    {
                        newBuffer.Use();
                        DoPostEffect(ColorCorrectionShader, currentBuffer.ColorBuffers[0]);
                        currentBuffer = newBuffer;
                    }

                    if (useFXAA)
                    {
                        using (new DebugGroup("FXAA"))
                        {
                            newBuffer = PostMan.NextFramebuffer;
                            newBuffer.Use();
                            DoPostEffect(FXAAShader, currentBuffer.ColorBuffers[0]);
                            currentBuffer = newBuffer;
                        }
                    }
                }
            }

            if (showBoundingBoxes)
            {
                using var bbGroup = new DebugGroup("Bounding Boxes");
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                foreach (var area in scene.VisibilityPortals)
                    area.RenderBoundingBox();
                foreach (var area in scene.VisibilityAreas.Values)
                    area.RenderBoundingBox();
                foreach (var model in scene.Models)
                    model.RenderBoundingBox();
                foreach (var area in VisibleAreas)
                    foreach (var model in area.Models)
                        model.RenderBoundingBox();

                GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
            }

            currentBuffer.BlitTo(finalBuffer, ClearBufferMask.ColorBufferBit);
        }

        private void GBufferPass(FrameBuffer finalBuffer)
        {
            using var debugGroup = new DebugGroup(nameof(GBufferPass));
            Debug.Assert(FrameBuffer.Current == GBuffers.Handle);
            using (GeoPassQuery = queryPool.BeginScope(QueryTarget.TimeElapsed))
            {
                using (new DebugGroup("Occluders"))
                {
                    foreach (var area in VisibleAreas)
                        area.RenderOccluderGeometry();
                    scene.RenderOccluderGeometry();
                }

                using (new DebugGroup("Non Occluders"))
                {
                    foreach (var area in VisibleAreas)
                        area.RenderNonOccluderGeometry();
                    scene.RenderNonOccluderGeometry();
                }

                DetermineVisibleAreas();

                scene.RenderTerrain();
            }
        }

        private void ReadbackQueries()
        {
            using var debugGroup = new DebugGroup("Readback queries");
            if (GeoPassQuery != null)
            {
                var time = GeoPassQuery.GetResult();
                //Console.WriteLine(time / 1000000f + "ms");
            }

            scene.BeforeFrame();
            foreach (var area in scene.VisibilityAreas.Values)
                area.BeforeFrame();
        }

        private void setupBuffers()
        {
            setupBloomUBO();

            setupRandomTexture();
        }

        private void setupBloomUBO()
        {
            var weights = new[,] {
                {1.00000f, 1.94883f, 1.75699f, 1.45802f, 1.11366f, 0.78296f, 0.50667f, 0.30179f, 0.16545f, 0.08349f, 0.03878f, 0.01658f, 0.00652f, 0.00236f, 0.00079f, 0.00024f, 0.00007f, 0.00002f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f},
                {1.00000f, 1.94883f, 1.75699f, 1.45802f, 1.11366f, 0.78296f, 0.50667f, 0.30179f, 0.16545f, 0.08349f, 0.03878f, 0.01658f, 0.00652f, 0.00236f, 0.00079f, 0.00024f, 0.00007f, 0.00002f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f},
                {1.00000f, 1.89943f, 1.54556f, 1.06639f, 0.62389f, 0.30950f, 0.13018f, 0.04643f, 0.01404f, 0.00360f, 0.00078f, 0.00014f, 0.00002f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f},
                {1.00000f, 1.89943f, 1.54556f, 1.06639f, 0.62389f, 0.30950f, 0.13018f, 0.04643f, 0.01404f, 0.00360f, 0.00078f, 0.00014f, 0.00002f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f},
                {1.00000f, 1.80567f, 1.20071f, 0.57600f, 0.19930f, 0.04972f, 0.00894f, 0.00116f, 0.00011f, 0.00001f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f},
                {1.00000f, 1.80567f, 1.20071f, 0.57600f, 0.19930f, 0.04972f, 0.00894f, 0.00116f, 0.00011f, 0.00001f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f}
            };
            var offsets = new[,] {
                {0.00000f, 0.00289f, 0.00675f, 0.01060f, 0.01446f, 0.01832f, 0.02217f, 0.02603f, 0.02988f, 0.03374f, 0.03760f, 0.04145f, 0.04531f, 0.04917f, 0.05302f, 0.05688f, 0.06074f, 0.06460f, 0.06846f, 0.07231f, 0.07617f, 0.08003f, 0.08389f, 0.08775f},
                {0.00000f, 0.00155f, 0.00363f, 0.00570f, 0.00777f, 0.00984f, 0.01192f, 0.01399f, 0.01606f, 0.01814f, 0.02021f, 0.02228f, 0.02435f, 0.02643f, 0.02850f, 0.03057f, 0.03265f, 0.03472f, 0.03679f, 0.03887f, 0.04094f, 0.04302f, 0.04509f, 0.04717f},
                {0.00000f, 0.00575f, 0.01342f, 0.02110f, 0.02877f, 0.03644f, 0.04412f, 0.05179f, 0.05947f, 0.06715f, 0.07483f, 0.08252f, 0.09021f, 0.09789f, 0.10559f, 0.11328f, 0.12098f, 0.12868f, 0.13638f, 0.14408f, 0.15179f, 0.15950f, 0.16721f, 0.17492f},
                {0.00000f, 0.00309f, 0.00722f, 0.01134f, 0.01546f, 0.01959f, 0.02371f, 0.02784f, 0.03197f, 0.03609f, 0.04022f, 0.04435f, 0.04849f, 0.05262f, 0.05675f, 0.06089f, 0.06503f, 0.06916f, 0.07330f, 0.07744f, 0.08159f, 0.08573f, 0.08988f, 0.09402f},
                {0.00000f, 0.01139f, 0.02657f, 0.04176f, 0.05697f, 0.07218f, 0.08742f, 0.10268f, 0.11795f, 0.13325f, 0.14856f, 0.16390f, 0.17925f, 0.19463f, 0.21001f, 0.22542f, 0.24083f, 0.25626f, 0.27170f, 0.28715f, 0.30260f, 0.31807f, 0.33353f, 0.34901f},
                {0.00000f, 0.00612f, 0.01428f, 0.02245f, 0.03062f, 0.03880f, 0.04699f, 0.05519f, 0.06340f, 0.07162f, 0.07985f, 0.08810f, 0.09635f, 0.10461f, 0.11288f, 0.12116f, 0.12945f, 0.13774f, 0.14604f, 0.15434f, 0.16265f, 0.17096f, 0.17927f, 0.18759f}
            };

            var structs = new List<float>();
            for (int y = 0; y < 6; y++)
            {
                for (int x = 0; x < 24; x++)
                {
                    structs.Add(weights[y, x]);
                    structs.Add(offsets[y, x]);
                }
                while (sizeof(float) * structs.Count % Globals.UniformBufferOffsetAlignment != 0)
                    structs.Add(0);
                bloomDataStride = Math.Min(sizeof(float) * structs.Count, bloomDataStride);
            }

            var data = structs.ToArray();
            bloomBuffer = new Buffer<float>(data, BufferTarget.UniformBuffer, BufferUsageHint.StreamDraw, "BloomData");
        }

        private void setupRandomTexture()
        {
            const int randomTextureSize = 64;
            const int randomTexturePixels = randomTextureSize * randomTextureSize;
            var data = new byte[randomTexturePixels * 3];
            new Random().NextBytes(data);

            var texParams = new TextureParams()
            {
                GenerateMips = false,
                InternalFormat = PixelInternalFormat.Rgb,
                MagFilter = TextureMinFilter.Linear,
                MinFilter = TextureMinFilter.Linear,
                WrapMode = TextureWrapMode.Repeat,
                Name = "Random",
                PixelFormat = PixelFormat.Rgb,
                Data = Marshal.UnsafeAddrOfPinnedArrayElement(data, 0)
            };
            NoiseMap = new Texture2D(randomTextureSize, randomTextureSize, texParams);
        }

        private void ResolveGBuffer(FrameBuffer outputBuffer)
        {
            GL.Disable(EnableCap.DepthTest);
            //GL.DepthMask(false);

            DoLightPass(new Vector3(0.01f));

            if (debugLightBuffer) {
                GL.Enable(EnableCap.FramebufferSrgb);
                outputBuffer.Use();

                DoPostEffect(FullBrightShader, LightingBuffer.ColorBuffers[0]);

                GL.Disable(EnableCap.FramebufferSrgb);
            } else {
                var currentBuffer = PostMan.NextFramebuffer;
                currentBuffer.Use();

                using (new DebugGroup("Light Resolve"))
                {
                    var shader = FinalCombineShader;
                    shader.Use();
                    LightingBuffer.ColorBuffers[0].Use(TextureUnit.Texture0);
                    GBuffers.ColorBuffers[0].Use(TextureUnit.Texture1);
                    shader.Set("texture0", TextureUnit.Texture0);
                    shader.Set("texture1", TextureUnit.Texture1);
                    shader.Set("frame", (int)FrameNumber);
                    shader.Set("resolution", new Vector2(frameBufferWidth, frameBufferHeight));
                    Primitives.Quad.Draw();
                }

                // Blit to output frame buffer
                currentBuffer.BlitTo(outputBuffer, ClearBufferMask.ColorBufferBit);
            }

            // Blit depth to output frame buffer
            GBuffers.BlitTo(outputBuffer, ClearBufferMask.DepthBufferBit);

            //GL.DepthMask(true);
            GL.Enable(EnableCap.DepthTest);
        }

        private FrameBuffer DoBloomPass(Texture diffuse)
        {
            using (new DebugGroup("Bloom"))
            {
                // Extract bright parts
                var currentFB = PostMan.NextFramebuffer;
                currentFB.Use();
                ExtractBright.Use();
                diffuse.Use(TextureUnit.Texture0);
                ExtractBright.Set("diffuseMap", TextureUnit.Texture0);
                ExtractBright.Set("avInvScreenSize", new Vector2(1f / frameBufferWidth, 1f / frameBufferHeight));
                ExtractBright.Set("afBrightPass", 15f);
                Primitives.Quad.Draw();

                // Take bright parts and blur
                bloomBuffer.Bind();

                int width = frameBufferWidth,
                    height = frameBufferHeight;
                var previousTexture = currentFB.ColorBuffers[0];
                Shader shader;
                for (var i = 0; i < BloomBuffers.Length;)
                {
                    width /= 2;
                    height /= 2;
                    GL.Viewport(0, 0, width, height);

                    var shaderSteps = new[] { VerticalBlurShader, HorizontalBlurShader };
                    foreach (var step in shaderSteps)
                    {
                        shader = step;
                        shader.Use();
                        BloomBuffers[i].Use();

                        bloomBuffer.BindRange(bloomDataStride * i, 3);

                        DoPostEffect(shader, previousTexture);

                        previousTexture = BloomBuffers[i].ColorBuffers[0];
                        i++;
                    }
                }

                // Add all to finished frame
                GL.Viewport(0, 0, frameBufferWidth, frameBufferHeight);
                currentFB = PostMan.NextFramebuffer;
                currentFB.Use();
                GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);

                shader = BloomCombineShader;
                shader.Use();
                Texture.Use(new[] { BloomBuffers[1].ColorBuffers[0], BloomBuffers[3].ColorBuffers[0], BloomBuffers[5].ColorBuffers[0], NoiseMap }, TextureUnit.Texture0);
                shader.Set("blurMap0", TextureUnit.Texture0);
                shader.Set("blurMap1", TextureUnit.Texture1);
                shader.Set("blurMap2", TextureUnit.Texture2);
                shader.Set("noiseMap", TextureUnit.Texture3);
                shader.Set("avInvScreenSize", new Vector2(1f / frameBufferWidth, 1f / frameBufferHeight));
                shader.Set("timeMilliseconds", elapsedMilliseconds);
                Primitives.Quad.Draw();

                GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
                GL.Disable(EnableCap.FramebufferSrgb);

                return currentFB;
            }
        }

        private void DisplayGBuffer(FrameBuffer finalBuffer, int buffer)
        {
            GL.Enable(EnableCap.FramebufferSrgb);
            finalBuffer.Use();

            DoPostEffect(FullBrightShader, GBuffers.ColorBuffers[buffer]);

            GL.Disable(EnableCap.FramebufferSrgb);
        }

        private void DoPostEffect(Shader shader, Texture input)
        {
            shader.Use();
            input.Use();
            shader.Set("texture0", 0);
            Primitives.Quad.Draw();
        }

        public void DoLightPass(Vector3 ambientColor)
        {
            using (new DebugGroup("Lighting"))
            {
                LightingBuffer.Use();
                GL.ClearColor(ambientColor.X, ambientColor.Y, ambientColor.Z, 0);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
                GL.CullFace(CullFaceMode.Front);

                RenderLights();

                GL.CullFace(CullFaceMode.Back);
                GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
                GL.BlendEquation(BlendEquationMode.FuncReverseSubtract);

                if (useSSAO)
                    SSAOPostEffect(); // TODO: Fix

                GL.BlendEquation(BlendEquationMode.FuncAdd);
                GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
            }
        }

        private void RenderLights()
        {
            var gbuffers = new[] {
                GBuffers.ColorBuffers[(int)GBufferTexture.Diffuse],
                GBuffers.ColorBuffers[(int)GBufferTexture.Position],
                GBuffers.ColorBuffers[(int)GBufferTexture.Normal],
                GBuffers.ColorBuffers[(int)GBufferTexture.Specular],
            };
            scene.RenderLights(
                frustumMaterial,
                SpotLightShader,
                PointLightShader,
                singleColorMaterial,
                gbuffers,
                debugLights
            );
            foreach (var area in VisibleAreas)
            {
                area.RenderLights(
                    frustumMaterial,
                    SpotLightShader,
                    PointLightShader,
                    singleColorMaterial,
                    gbuffers,
                    debugLights
                );
            }
        }

        private void SSAOPostEffect()
        {
            using var debugGroup = new DebugGroup("SSAO");
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

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            Camera.Update(KeyboardState);

            if (IsFocused) {
                var input = KeyboardState;

                if (input.IsKeyPressed(Keys.L))
                    debugLights = !debugLights;

                if (input.IsKeyDown(Keys.X))
                    HPLEntity.Offset.X += 0.01f;
                if (input.IsKeyDown(Keys.Y))
                    HPLEntity.Offset.Y += 0.01f;
                if (input.IsKeyDown(Keys.Z))
                    HPLEntity.Offset.Z += 0.01f;

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

                if (input.IsKeyReleased(Keys.D9))
                    debugLightBuffer = !debugLightBuffer;

                if (input.IsKeyReleased(Keys.F))
                    useFXAA = !useFXAA;

                if (input.IsKeyReleased(Keys.O))
                    useSSAO = !useSSAO;

                if (input.IsKeyReleased(Keys.G))
                    enableBloom = !enableBloom;

                if (input.IsKeyPressed(Keys.V))
                    VSync = VSync == VSyncMode.Off ? VSyncMode.On : VSyncMode.Off;

                if (input.IsKeyPressed(Keys.B))
                    showBoundingBoxes = !showBoundingBoxes;
            }
        }

        protected override void OnClosing(CancelEventArgs e) {
            Mouse.Grabbed = false;

            base.OnClosing(e);
        }

        public enum GBufferTexture
        {
            Diffuse,
            Position,
            Normal,
            Specular
        }
    }
}
