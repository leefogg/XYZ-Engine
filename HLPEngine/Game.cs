using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Assimp;
using GLOOP.Rendering;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TextureWrapMode = OpenTK.Graphics.OpenGL4.TextureWrapMode;
using GLOOP.Rendering.Materials;
using GLOOP.HPL.Loading;
using HLPEngine;
using Valve.VR;
using System.Diagnostics;
using GLOOP.Rendering.Debugging;
using ImGuiNET;
using Primitives = GLOOP.Rendering.Primitives;
using GLOOP.Util;
using GLOOP.Util.Structures;
using System.Text;
using System.IO;
using PrimitiveType = OpenTK.Graphics.OpenGL4.PrimitiveType;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using GLOOP.DAE;

namespace GLOOP.HPL
{
    public class Game : Window {
        public enum GBufferTexture
        {
            Diffuse,
            Position,
            Normal,
            Specular
        }

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        private readonly struct BlurData
        {
            [FieldOffset(00)] public readonly float Weight;
            [FieldOffset(04)] public readonly float Offset;
            // +8 more bytes of padding

            public BlurData(float weight, float offset)
            {
                Weight = weight;
                Offset = offset;
            }
            public override string ToString() => $"Weight:{Weight}, Offset:{Offset}";
        }

        private readonly struct MapSetup
        {
            public readonly string Path;
            public readonly Vector3 CameraPos;

            public MapSetup(string path, Vector3 cameraPos)
            {
                Path = path;
                CameraPos = cameraPos;
            }
        }

        private const string lab = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter00\00_03_laboratory\00_03_laboratory.hpm";
        private const string theta_outside = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter02\02_04_theta_outside\02_04_theta_outside.hpm";
        private const string upsilon = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter01\01_02_upsilon_inside\01_02_upsilon_inside.hpm";
        private const string theta_inside = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter02\02_05_theta_inside\02_05_theta_inside.hpm";
        private const string boundingBoxes = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\Testing\BoundingBoxes\BoundingBoxes.hpm";
        private const string terrain = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\Testing\Terrain\Terrain.hpm";
        private const string Box3Contains = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\Testing\Box3Contains\Box3Contains.hpm";
        private static readonly MapSetup Custom = new MapSetup(@"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\custom\custom.hpm", new Vector3(6.3353596f, 1.6000088f, 8.1601305f));
        private static readonly MapSetup Phi = new MapSetup(@"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter05\05_01_phi_inside\05_01_phi_inside.hpm", new Vector3(-17.039896f, 14.750014f, 64.48185f));
        private static readonly MapSetup Delta = new MapSetup(@"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter02\02_03_delta\02_03_delta.hpm", new Vector3(0, 145, -10));
        private static readonly MapSetup Lights = new MapSetup(@"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\Testing\Lights\Lights.hpm", new Vector3(-0.5143715f, 4.3500123f, 11.639848f));
        private static readonly MapSetup Portals = new MapSetup(@"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\Testing\Portals\Portals.hpm", new Vector3(4.5954947f, 1.85f, 16.95526f));
        private static readonly MapSetup ThetaInsideLab = new MapSetup(@"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter02\02_05_theta_inside\02_05_theta_inside.hpm", new Vector3(0,5,0));
        private static readonly MapSetup ThetaTunnels = new MapSetup(@"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter02\02_06_theta_tunnels\02_06_theta_tunnels.hpm", new Vector3(4, 9, -61));
        private static readonly MapSetup ThetaExit = new MapSetup(@"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter02\02_07_theta_exit\02_07_theta_exit.hpm", new Vector3(11.340768f, 1.6000444f, 47.520298f));
        private static readonly MapSetup Wau = new MapSetup(@"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter04\04_03_tau_escape\04_03_tau_escape.hpm", new Vector3(-26.12f, 93.691f, 167.313f));
        private static readonly MapSetup UpsilonAwake = new MapSetup(@"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter01\01_01_upsilon_awake\01_01_upsilon_awake.hpm", new Vector3(9.325157f, -0.44998702f, 50.61429f));
        private static readonly MapSetup Upsilon = new MapSetup(@"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter01\01_02_upsilon_inside\01_02_upsilon_inside.hpm", new Vector3(9.325157f, -0.44998702f, 50.61429f));
        private static readonly MapSetup Bedroom = new MapSetup(@"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter00\00_01_apartment\00_01_apartment.hpm", new Vector3(-11.600799f, 1.4500086f, 11.624353f));
        private static readonly MapSetup Omicron = new MapSetup(@"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter03\03_02_omicron_inside\03_02_omicron_inside.hpm", new Vector3(-1.0284736f, -2.0497713f, 21.69069f));
        private static readonly MapSetup TauOutside = new MapSetup(@"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter04\04_01_tau_outside\04_01_tau_outside.hpm", new Vector3(77.65444f, 315.97113f, -340.09308f));
        private static readonly MapSetup Tau = new MapSetup(@"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter04\04_02_tau_inside\04_02_tau_inside.hpm", new Vector3(26.263678f, 1.7000114f, 36.090767f));
#if PROFILE
        private readonly MapSetup MapToUse = Phi;
#else
        private readonly MapSetup MapToUse = Custom;
#endif

        private Camera Camera;
        private Scene scene;

        private FrameBuffer GBuffers;
        private FrameBuffer LightingBuffer;
        private FrameBuffer[] BloomBuffers;
#if VR
        private FrameBuffer LeftEyeBuffer;
        private FrameBuffer RightEyeBuffer;
#endif
        private Shader PointLightShader;
        private Shader SpotLightShader;
        private Texture2D RGBNoiseMap, BWNoiseMap;
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
        private Shader FinalCombine;
        private FrustumMaterial frustumMaterial;
        private Texture2D DirtTexture, CrackTexture;
        private QueryPool queryPool;
        private List<(VisibilityPortal, ScopedQuery)> PortalQueries = new List<(VisibilityPortal, ScopedQuery)>();
        private List<VisibilityArea> VisibleAreas = new List<VisibilityArea>();
        private Buffer<BlurData> bloomBuffer;

        // Lighting
        private float OffsetByNormalScalar = 0.05f;
        private float LightBrightnessMultiplier = 1f;
        private float SpecularPowerScalar = 8f;
        private float LightingScalar = 8.0f;
        private float DiffuseScalar = 1.0f;
        private float SpecularScalar = 1.0f;
        private bool UseLightDiffuse = true;
        private bool UseLightSpecular = true;
        private float LightScatterScalar = 0.1f;
        private float NoiseScalar = 0.01f;
        // Bloom
        private float BrightPass = 0.95f;
        private float BrightScalar = 0.5f;
        private System.Numerics.Vector3 SizeWeight = new System.Numerics.Vector3(0.5f, 0.75f, 1f);
        private float WeightScalar = 1.75f;
        private float BlurWidthPercent = 5f, BlurPixelOffset = 0.75f;
        private int NumBlurSamples = 6;
        private int BlurStrideElements;
#if DEBUG
        private float DirtHighlightScalar = 0;
        private float DirtGeneralScalar = 0;
        private float CrackScalar = 0;
#else
        private float DirtHighlightScalar = 0.5f;
        private float DirtGeneralScalar = 0.01f;
        private float CrackScalar = 0.005f;
#endif
        // Post
        private float Key = 1f;
        private float Exposure = 1f;
        private float Gamma = 2.4f;
        private float WhiteCut = 1f;
        // SSAO
        private int MinSSAOSamples = 8;
        private int MaxSSAOSamples = 32;
        private float MaxSamplesDistance = 0.5f;
        private float Intensity = 8f;
        private float Bias = 0.5f;
        private float SampleRadius = 0.005f;
        private float MaxDistance = 0.1f;
        // ImGUI variables
        private bool debugLights;
        private int debugGBufferTexture = -1;
        private bool debugLightBuffer;
        private bool enableFXAA = true;
        private bool enableSSAO = false;
        private bool enableBloom = true;
        private bool showBoundingBoxes = false;
        private bool showSkeletons = false;
        private bool enablePortalCulling = true;
        private bool enableImGui = false;
        private bool shouldUpdateVisibility = true;

        private float elapsedMilliseconds = 0;
        private CPUProfiler.Frame CPUFrame;
        private GPUProfiler.Frame GPUFrame;
        private Task VisibilityTask;
        private readonly DateTime startTime = DateTime.Now;
        private readonly FrameBuffer backBuffer;
        private readonly int frameBufferWidth, frameBufferHeight;
        private readonly ImGuiController ImGuiController;
        private readonly Ring<float> CPUFrameTimings = new Ring<float>(PowerOfTwo.OneHundrendAndTwentyEight);
        private readonly Ring<float> GPUFrameTimings = new Ring<float>(PowerOfTwo.OneHundrendAndTwentyEight);
        //private readonly StringBuilder CSV = new StringBuilder(10000);
#if !PROFILE
        private const bool BenchmarkMode = false;
#else
        private const bool BenchmarkMode = true;
#endif
        private DebugLineRenderer LineRenderer;

#region Setup

        public Game(int width, int height, GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings) 
        {
            ICameraController controller;
            if (BenchmarkMode)
            {
                var c = new PlaybackCameraController("Phi.csv");
                c.OnRecordingComplete = OnBenchmarkComplete;
                controller = c;

                enableImGui = true;
                enableFXAA = true;
                enableBloom = true;
                enableSSAO = true;
                VSync = VSyncMode.Off;
            }
            else
            {
               controller = new PCCameraController();
            }
            Camera = new Camera(MapToUse.CameraPos, new Vector3(), 90)
            {
                Width = width,
                Height = height,
                CameraController = controller,
            };

            Camera.Current = Camera;

            backBuffer = new FrameBuffer(0, Size.X, Size.Y);
            frameBufferWidth = width;
            frameBufferHeight = height;

#if VR
            DebugCamera.MAX_LOOK_UP = DebugCamera.MAX_LOOK_DOWN = 0;
            enableFXAA = false;
            enableSSAO = false;
            enableBloom = false;
#endif
#if !RELEASE
            ImGuiController = new ImGuiController(ClientSize.X, ClientSize.Y);
            LineRenderer = new DebugLineRenderer(128);
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
                    PixelInternalFormat.Rgba16f,    // Position
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
#endif
            PostMan.Init(width, height, PixelInternalFormat.Rgb16f);

#region Shaders
            var deferredMaterial = new DeferredRenderingGeoMaterial();
            PointLightShader = new StaticPixelShader(
                "assets/shaders/deferred/LightPass/VertexShader.vert",
                "assets/shaders/deferred/LightPass/FragmentShader.frag",
                new Dictionary<string, string>
                {
                    { "LIGHTTYPE", "0" }
                },
                "Deferred Point light"
            );
            SpotLightShader = new StaticPixelShader(
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
                null,
                "Final combine"
            );
            FullBrightShader = new StaticPixelShader(
                "assets/shaders/FullBright/2D/VertexShader.vert",
                "assets/shaders/FullBright/2D/FragmentShader.frag",
                null,
                "Fullbright"
            );
            ExtractBright = new StaticPixelShader(
                "assets/shaders/Bloom/Extract/vertex.vert",
                "assets/shaders/Bloom/Extract/fragment.frag",
                null,
                "Extract brightness"
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
            FinalCombine = new DynamicPixelShader(
                 "assets/shaders/PostEffects/vertex.vert",
                "assets/shaders/PostEffects/fragment.frag",
                null,
                "Final Combine"
            );
#endregion
            
            frustumMaterial = new FrustumMaterial(new FrustumShader());
            queryPool = new QueryPool(8);

            var assimp = new AssimpContext();
            var beforeMapLoad = DateTime.Now;
            var map = new Map(
                MapToUse.Path,
                assimp,
                deferredMaterial
            );
            scene = map.ToScene();
            var afterMapLoad = DateTime.Now;

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

            VisibleAreas.AddRange(scene.VisibilityAreas.Values); 

            SetupBuffers();

            /*
            var usedTextures = TextureArrayManager.GetSummary();
            foreach (var alloc in usedTextures)
                Console.WriteLine($"Shape {alloc.Shape} used {alloc.AllocatedSlices} slices");
            Directory.CreateDirectory("meta");
            var summaryJson = JsonConvert.SerializeObject(usedTextures);
            File.WriteAllText(metaFilePath, summaryJson);
            */
        }

        private void CreateRandomRGBTexture()
        {
            const int randomTextureSize = 64;
            const int randomTexturePixels = randomTextureSize * randomTextureSize;
            var data = new byte[randomTexturePixels * 3];
            new Random().NextBytes(data);

            var texParams = new TextureParams()
            {
                GenerateMips = false,
                InternalFormat = PixelInternalFormat.Rgb,
                MagFilter = TextureMinFilter.Nearest,
                MinFilter = TextureMinFilter.Nearest,
                WrapMode = TextureWrapMode.Repeat,
                Name = "Random RGB",
                PixelFormat = PixelFormat.Rgb,
                Data = Marshal.UnsafeAddrOfPinnedArrayElement(data, 0)
            };
            RGBNoiseMap = new Texture2D(randomTextureSize, randomTextureSize, texParams);
        }

        private void CreateRandomBWTexture()
        {
            const int randomTextureSize = 64;
            const int randomTexturePixels = randomTextureSize * randomTextureSize;
            var data = new byte[randomTexturePixels];
            new Random().NextBytes(data);

            var texParams = new TextureParams()
            {
                GenerateMips = false,
                InternalFormat = PixelInternalFormat.CompressedRed,
                MagFilter = TextureMinFilter.Nearest,
                MinFilter = TextureMinFilter.Nearest,
                WrapMode = TextureWrapMode.Repeat,
                Name = "Random Red",
                PixelFormat = PixelFormat.Red,
                Data = Marshal.UnsafeAddrOfPinnedArrayElement(data, 0)
            };
            BWNoiseMap = new Texture2D(randomTextureSize, randomTextureSize, texParams);
        }

        private void SetupBuffers()
        {
            scene.SetupBuffers();

            CreateRandomRGBTexture();
            CreateRandomBWTexture();
            DirtTexture = new Texture2D("assets/textures/Scratches.png", new TextureParams()
            {
                InternalFormat = PixelInternalFormat.R8,
                GenerateMips = false,
                MinFilter = TextureMinFilter.Nearest,
                MagFilter = TextureMinFilter.Nearest,
                Name = "Dirt"
            });
            CrackTexture = new Texture2D("assets/textures/Crack.jpg", new TextureParams()
            {
                InternalFormat = PixelInternalFormat.Rgb,
                GenerateMips = false,
                MinFilter = TextureMinFilter.Linear,
                MagFilter = TextureMinFilter.Linear,
                Name = "Crack"
            });

            UpdateBloomBuffer();
        }

#endregion

#region Game Loop
        protected override void OnRenderFrame(FrameEventArgs args)
        {
            using var cpuFrame = CPUProfiler.NextFrame;
            CPUFrame = cpuFrame;

            FrameStart();

            float frameElapsedMs = 0;
            using (var query = queryPool.BeginScope(QueryTarget.TimeElapsed))
            {
                using var gpuFrame = GPUProfiler.NextFrame;
                GPUFrame = gpuFrame;
                var frameNo = Window.FrameNumber;
                TaskMaster.AddTask(
                    query.IsResultAvailable,
                    () => {
                        GPUFrameTimings.Set(1000f / (query.GetResult() / 1000000f)); 
                        Metrics.WriteLog(frameNo, cpuFrame, gpuFrame);
                        GPUFrameTimings.MoveNext();
                    },
                    "Frame timing query"
                );

                Metrics.ResetFrameCounters();

                var frameStart = DateTime.Now;

                ImGuiController?.Update(this, (float)args.Time);

                GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
                GL.Viewport(0, 0, Size.X, Size.Y);

#if VR
            VRSystem.UpdateEyes();
            VRSystem.UpdatePoses();
               
            if (FrameNumber == 1)
                VRSystem.SetOriginHeadTransform();

            // Left Eye
            updateCameraUBO(
                VRSystem.GetEyeProjectionMatrix(EVREye.Eye_Left),
                Camera.ViewMatrix * VRSystem.GetEyeViewMatrix(EVREye.Eye_Left)
            );
            ResetGBuffer();
            VRSystem.RenderEyeHiddenAreaMesh(EVREye.Eye_Left);
            RenderPass(LeftEyeBuffer);
            VRSystem.SubmitEye(LeftEyeBuffer.ColorBuffers[0], EVREye.Eye_Left);

            // Right Eye
            {
                using var timer = CurrentFrame[FrameProfiler.Event.UpdateBuffers];
                updateCameraUBO(
                    VRSystem.GetEyeProjectionMatrix(EVREye.Eye_Right),
                    Camera.ViewMatrix * VRSystem.GetEyeViewMatrix(EVREye.Eye_Right)
                );
            }
            ResetGBuffer();
            VRSystem.RenderEyeHiddenAreaMesh(EVREye.Eye_Right);
            RenderPass(RightEyeBuffer);
            VRSystem.SubmitEye(RightEyeBuffer.ColorBuffers[0], EVREye.Eye_Right);

            RightEyeBuffer.BlitTo(backBuffer, ClearBufferMask.ColorBufferBit);
#else
                ResetGBuffer();
                RenderPass(backBuffer);
#endif

                UpdateBuffers();

                UpdateScene();

                DrawImGUIWindows();

                if (BenchmarkMode && FrameNumber == 1)
                    Metrics.StartRecording($"{DateTime.Now:ddMMyyyy HHmm}.csv");

                frameElapsedMs = (float)(DateTime.Now - frameStart).TotalMilliseconds;
                CPUFrameTimings.SetAndMove(frameElapsedMs);
                EventProfiler.NewFrame();

            }

            SwapBuffers();

            FrameEnd();

            elapsedMilliseconds = (float)(DateTime.Now - startTime).TotalMilliseconds;
        }

        private void ResetGBuffer()
        {
            GBuffers.Use();
            var clearColor = debugLights ? 0.2f : 0;
            GL.ClearColor(clearColor, clearColor, clearColor, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Viewport(0, 0, frameBufferWidth, frameBufferHeight);
        }

        private void RenderPass(FrameBuffer backbuffer)
        {
            GL.Viewport(0, 0, frameBufferWidth, frameBufferHeight);

            var currentBuffer = PostMan.NextFramebuffer;

            GeometryPass(currentBuffer);

            DoPostEffects(backbuffer);

            //BlitToBackBuffer(backbuffer, currentBuffer);

            RenderSkeletons();
            RenderBoundingBoxes();
        }

        private void GeometryPass(FrameBuffer finalBuffer)
        {
            using var profiler = EventProfiler.Profile();
            Debug.Assert(FrameBuffer.Current == GBuffers.Handle);

            {
                using var cpuTimer = CPUFrame[CPUProfiler.Event.Geomertry];
                using var gpuTimer = GPUFrame[GPUProfiler.Event.Geomertry];
                using var debugGroup = new DebugGroup(nameof(GeometryPass));

                GL.Enable(EnableCap.FramebufferSrgb);
                scene.RenderModels();
                scene.RenderTerrain();
                GL.Disable(EnableCap.FramebufferSrgb);
            }

            DetermineVisibleAreas();

        }

        private void UpdateBuffers()
        {
            using var profiler = EventProfiler.Profile();
            using var cpuTimer = CPUFrame[CPUProfiler.Event.UpdateBuffers];
            using var gpuTimer = GPUFrame[GPUProfiler.Event.UpdateBuffers];

            if (VisibilityTask?.IsCompleted ?? true)
            {
                VisibilityTask = Task.Run(UpdateVisibility);
                if (shouldUpdateVisibility || FrameNumber == 0)
                    scene.UpdateBuffers();
            }

            updateCameraUBO(Camera.ProjectionMatrix, Camera.ViewMatrix);

#if DEBUG
            UpdateBloomBuffer();
#endif
        }

        public void UpdateScene()
        {
            scene.UpdateSkinnedModels();
        }

        private void UpdateVisibility()
        {
            if (!(shouldUpdateVisibility || FrameNumber == 0))
                return;

            scene.UpdateVisibility(VisibleAreas.Cast<RenderableArea>().ToList());
        }

        private void DetermineVisibleAreas()
        {
            using var profiler = EventProfiler.Profile();
            using var timer = CPUFrame[CPUProfiler.Event.PortalCulling];

            if (!shouldUpdateVisibility)
                return;

            if (!enablePortalCulling)
            {
                VisibleAreas.Clear();
                VisibleAreas.AddRange(scene.VisibilityAreas.Values);
                return;
            }

            // Dont need to check every frame
            // Also must only run every odd frame for VR support
#if VR
            if ((FrameNumber & 1) != 0)
                return;
#endif

            VisibleAreas.Clear();
            // Get touching areas
            // This should be first to render closer areas first
            // "A visibility area that is connected to a portal will only be visible if the portal is visible or if the camera is inside it."
            VisibleAreas.AddRange(scene.VisibilityAreas.Values.Where(area => area.BoundingBox.Contains(Camera.Position)));
            if (VisibleAreas.Any())
            {
                // To avoid seams, always consider all connecting areas of touching portals to be visible
                // "If the camera is inside of a visibility area then all the portals connected to it will be used as a portal to the outside."
                var touchingPortals = scene.VisibilityPortals.Where(area => area.BoundingBox.Contains(Camera.Position));
                foreach (var portal in touchingPortals)
                    foreach (var areaName in portal.VisibilityAreas)
                        VisibleAreas.Add(scene.VisibilityAreas[areaName]);

                if (PortalQueries.Any(pair => !pair.Item2.IsResultAvailable()))
                    return;
                foreach (var (portal, query) in PortalQueries)
                {
                    portal.IsVisible = query.GetResult() > 0;
                    if (portal.IsVisible)
                        VisibleAreas.AddRange(portal.VisibilityAreas.Select(areaName => scene.VisibilityAreas[areaName]));
                }

                PortalQueries.Clear();

                using (new DebugGroup("Portals"))
                {
                    // Render portals
                    GL.ColorMask(false, false, false, false);
                    GL.Disable(EnableCap.CullFace);
                    GL.DepthMask(false);
                    // Dispatch queries for rooms visible from previous queries and current areas
                    foreach (var portal in VisibleAreas.SelectMany(area => area.ConnectingPortals).Distinct())
                    {
                        using var query = queryPool.BeginScope(QueryTarget.AnySamplesPassed);
                        Draw.Box(portal.ModelMatrix, new Vector4(1, 1, 0, 0));
                        PortalQueries.Add((portal, query));
                    }
                    GL.ColorMask(true, true, true, true);
                    GL.DepthMask(true);
                    GL.Enable(EnableCap.CullFace);
                }

                VisibleAreas = VisibleAreas.Distinct().ToList(); // Remove duplicates
            }
            else
                VisibleAreas.AddRange(scene.VisibilityAreas.Values); // If we're flying around outside any area, just show them all

        }

        private void DoPostEffects(FrameBuffer destination)
        {
            DoLightPass(new Vector3(0.00f));

            using (GPUFrame[GPUProfiler.Event.Post])
            {
                if (debugGBufferTexture > -1)
                {
                    if (debugGBufferTexture == 0 || debugGBufferTexture == 3)
                        GL.Enable(EnableCap.FramebufferSrgb);
                    destination.Use();
                    DoPostEffect(FullBrightShader, GBuffers.ColorBuffers[debugGBufferTexture]);
                    GL.Disable(EnableCap.FramebufferSrgb);
                }
                else if (debugLightBuffer)
                {
                    destination.Use();
                    GL.Enable(EnableCap.FramebufferSrgb);
                    DoPostEffect(FullBrightShader, LightingBuffer.ColorBuffers[0]);
                    GL.Disable(EnableCap.FramebufferSrgb);
                }
                else
                {
                    if (enableBloom)
                        ExtractBloom(GBuffers.ColorBuffers[(int)GBufferTexture.Diffuse], LightingBuffer.ColorBuffers[0]);

                    destination.Use();
                    var shader = FinalCombine;
                    shader.Use();
                    shader.Set("EnableSSAO", enableSSAO);
                    shader.Set("EnableFXAA", enableFXAA);
                    shader.Set("EnableBloom", enableBloom);
                    //Texture.Use(new[] {
                    //    GBuffers.ColorBuffers[(int)GBufferTexture.Diffuse],
                    //    GBuffers.ColorBuffers[(int)GBufferTexture.Position],
                    //    GBuffers.ColorBuffers[(int)GBufferTexture.Normal],
                    //    LightingBuffer.ColorBuffers[0],
                    //    BloomBuffers[1].ColorBuffers[0],
                    //    BloomBuffers[3].ColorBuffers[0],
                    //    BloomBuffers[5].ColorBuffers[0],
                    //    DirtTexture,
                    //    CrackTexture,
                    //}, TextureUnit.Texture0);
                    GBuffers.ColorBuffers[(int)GBufferTexture.Diffuse].Use(TextureUnit.Texture0);
                    shader.Set("diffuseMap", TextureUnit.Texture0);
                    LightingBuffer.ColorBuffers[0].Use(TextureUnit.Texture3);
                    shader.Set("lightMap", TextureUnit.Texture3);
                    BWNoiseMap.Use(TextureUnit.Texture9);
                    shader.Set("noiseMap", TextureUnit.Texture9);
                    // Bloom
                    //shader.Set("avSizeWeight", SizeWeight);
                    BloomBuffers[1].ColorBuffers[0].Use(TextureUnit.Texture4);
                    shader.Set("blurMap0", TextureUnit.Texture4);
                    BloomBuffers[3].ColorBuffers[0].Use(TextureUnit.Texture5);
                    shader.Set("blurMap1", TextureUnit.Texture5);
                    BloomBuffers[5].ColorBuffers[0].Use(TextureUnit.Texture6);
                    shader.Set("blurMap2", TextureUnit.Texture6);
                    DirtTexture.Use(TextureUnit.Texture7);
                    shader.Set("dirtMap", TextureUnit.Texture7);
                    CrackTexture.Use(TextureUnit.Texture8);
                    shader.Set("crackMap", TextureUnit.Texture8);
                    shader.Set("dirtHighlightScalar", DirtHighlightScalar);
                    shader.Set("dirtGeneralScalar", DirtGeneralScalar);
                    //shader.Set("crackScalar", CrackScalar);
                    // SSAO
                    GBuffers.ColorBuffers[(int)GBufferTexture.Position].Use(TextureUnit.Texture1);
                    shader.Set("positionTexture", TextureUnit.Texture1);
                    GBuffers.ColorBuffers[(int)GBufferTexture.Normal].Use(TextureUnit.Texture2);
                    shader.Set("normalTexture", TextureUnit.Texture2);
                    shader.Set("TimeMilliseconds", elapsedMilliseconds);
                    shader.Set("MinSamples", MinSSAOSamples);
                    shader.Set("MaxSamples", MaxSSAOSamples);
                    shader.Set("MaxSamplesDistance", MaxSamplesDistance);
                    shader.Set("Intensity", Intensity);
                    shader.Set("Bias", Bias);
                    shader.Set("SampleRadius", SampleRadius);
                    shader.Set("MaxDistance", MaxDistance);
                    // FXAA
                    shader.Set("Span", 8.0f);
                    // Noise
                    shader.Set("noiseScalar", NoiseScalar);
                    // Color Correction
                    shader.Set("afKey", Key);
                    shader.Set("afExposure", Exposure);
                    shader.Set("afInvGammaCorrection", 1f / Gamma);
                    shader.Set("afWhiteCut", WhiteCut);

                    Primitives.Quad.Draw();
                }
            }
        }

        private void DoPostEffect(Shader shader, Texture input)
        {
            shader.Use();
            input.Use();
            shader.Set("texture0", 0);
            Primitives.Quad.Draw();
        }

        void BlitToBackBuffer(FrameBuffer finalBuffer, FrameBuffer currentBuffer)
        {
            finalBuffer.Use();
            DoPostEffect(FullBrightShader, currentBuffer.ColorBuffers[0]);
        }

        private void ExtractBloom(Texture2D diffuse, Texture2D lightBuffer)
        {
            using var timer = CPUFrame[CPUProfiler.Event.Bloom];

            using (new DebugGroup("Bloom"))
            {
                var currentFB = PostMan.NextFramebuffer;
                using (new DebugGroup("Extract"))
                {
                    // Extract bright parts
                    currentFB.Use();
                    ExtractBright.Use();
                    diffuse.Use(TextureUnit.Texture0);
                    ExtractBright.Set("diffuseMap", TextureUnit.Texture0);
                    lightBuffer.Use(TextureUnit.Texture1);
                    ExtractBright.Set("lightMap", TextureUnit.Texture1);
                    ExtractBright.Set("afBrightPass", BrightPass);
                    ExtractBright.Set("afBrightScalar", BrightScalar);

                    Primitives.Quad.Draw();
                }

                using (new DebugGroup("Blur"))
                {
                    var previousTexture = currentFB.ColorBuffers[0];
                    Shader shader;
                    for (var i = 0; i < BloomBuffers.Length;)
                    {
                        var buffer = BloomBuffers[i];

                        GL.Viewport(0, 0, buffer.Width, buffer.Height);

                        var shaderSteps = new[] { VerticalBlurShader, HorizontalBlurShader };
                        foreach (var step in shaderSteps)
                        {
                            shader = step;
                            shader.Use();
                            shader.Set("NumSamples", NumBlurSamples);
                            BloomBuffers[i].Use();

                            bloomBuffer.Bind(Globals.BindingPoints.UBOs.Bloom, NumBlurSamples, BlurStrideElements * i);

                            DoPostEffect(shader, previousTexture);

                            previousTexture = buffer.ColorBuffers[0];
                            i++;
                        }
                    }
                }

                GL.Viewport(0, 0, frameBufferWidth, frameBufferHeight);
            }
        }

        private void UpdateBloomBuffer()
        {
            if (!enableBloom)
                return;

#if !DEBUG
            if (bloomBuffer != null)
                return;
#endif

            var sizeOfStruct = Marshal.SizeOf<BlurData>();
            Debug.Assert(Globals.UniformBufferOffsetAlignment % sizeOfStruct == 0, "BlurData cannot align to UniformBuffer Offset Alignment");

            var structs = new FastList<BlurData>(sizeOfStruct * MaxSSAOSamples);

            int scale = 1;
            for (int y = 0; y < 6; y++, scale *= 2)
            {
                float frameBufferSize, blurSize;
                blurSize = BlurWidthPercent;
                if ((y & 1) == 0) // Vertical or horizoncal blur pass
                {
                    frameBufferSize = frameBufferHeight;
                }
                else
                {
                    frameBufferSize = frameBufferWidth;
                }

                var sizePerPx = 1f / frameBufferSize;
                var pixelOffset = BlurPixelOffset * sizePerPx;
                var sizePerStep = (sizePerPx * blurSize) / NumBlurSamples;
                var invSamples = 1f / NumBlurSamples;

                for (int x = 0; x < NumBlurSamples; x++)
                {
                    structs.Add(new BlurData(
                        1f - invSamples * x,
                        (sizePerStep * blurSize * x) + pixelOffset
                    ));
                }
                while (sizeOfStruct * structs.Count % Globals.UniformBufferOffsetAlignment != 0)
                    structs.Add(new BlurData(0, 0));
                if (y == 0)
                    BlurStrideElements = structs.Count;
            }

            if (bloomBuffer == null)
                bloomBuffer = new Buffer<BlurData>(sizeOfStruct * structs.Count, BufferTarget.UniformBuffer, BufferUsageHint.StreamDraw, "BloomData");

            bloomBuffer.Update(structs.Elements, structs.Count, 0);

            var weights = structs.Elements.Select(s => s.Weight).ToArray();
            var offsets = structs.Elements.Select(s => s.Offset).ToArray();
            ImGui.PlotLines("Weights", ref weights[0], NumBlurSamples * 6, 0, string.Empty, 0, 1, new System.Numerics.Vector2(0, 50));
            ImGui.PlotLines("Offsets", ref offsets[0], NumBlurSamples * 6, 0, string.Empty, 0, .2f, new System.Numerics.Vector2(0, 50));

            ImGui.End();
        }

        public void DoLightPass(Vector3 ambientColor)
        {
            using var debugGroup = new DebugGroup("Lighting");
            using var gpuTimer = GPUFrame[GPUProfiler.Event.Lighting];
            using var timer = CPUFrame[CPUProfiler.Event.Lighting];

            LightingBuffer.Use();
            GL.ClearColor(ambientColor.X, ambientColor.Y, ambientColor.Z, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
            GL.CullFace(CullFaceMode.Front);
            RenderLights();
            GL.CullFace(CullFaceMode.Back);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
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
            shader.Set("Time", elapsedMilliseconds);

            shader.Set("MinSamples", MinSSAOSamples);
            shader.Set("MaxSamples", MaxSSAOSamples);
            shader.Set("MaxSamplesDistance", MaxSamplesDistance);
            shader.Set("Intensity", Intensity);
            shader.Set("Bias", Bias);
            shader.Set("SampleRadius", SampleRadius);
            shader.Set("MaxDistance", MaxDistance);

            GL.BlendEquation(BlendEquationMode.FuncReverseSubtract);
            Primitives.Quad.Draw();
            GL.BlendEquation(BlendEquationMode.FuncAdd);
        }

        private void RenderLights()
        {
            using var profiler = EventProfiler.Profile();

            var shaders = new[] { SpotLightShader, PointLightShader };
            foreach (var shader in shaders)
            {
                shader.Use();
                shader.Set("offsetByNormalScalar", OffsetByNormalScalar);
                shader.Set("lightBrightnessMultiplier", LightBrightnessMultiplier);
                shader.Set("specularPowerScalar", SpecularPowerScalar);
                shader.Set("lightingScalar", LightingScalar);
                shader.Set("diffuseScalar", DiffuseScalar);
                shader.Set("specularScalar", SpecularScalar);
                shader.Set("useLightSpecular", UseLightSpecular);
                shader.Set("useLightDiffuse", UseLightDiffuse);
                shader.Set("lightScatterScalar", LightScatterScalar);
            }

            var gbuffers = new[] { // TODO: Make this constant
                GBuffers.ColorBuffers[(int)GBufferTexture.Diffuse],
                GBuffers.ColorBuffers[(int)GBufferTexture.Position],
                GBuffers.ColorBuffers[(int)GBufferTexture.Normal],
                GBuffers.ColorBuffers[(int)GBufferTexture.Specular],
            };

            scene.RenderLights(
                singleColorMaterial,
                frustumMaterial,
                PointLightShader,
                SpotLightShader,
                gbuffers,
                debugLights
            );
        }

#endregion

#region ImGUI
        private void DrawImGUIWindows()
        {
            using var gpuTimer = GPUFrame[GPUProfiler.Event.ImGUI];
            using var cpuTimer = CPUFrame[CPUProfiler.Event.ImGUI];

            if (!enableImGui)
                return;

            DrawImGuiOptionsWindow();
            DrawImGuiMetricsWindow();
            DrawImGuiMaterialWindow();
            DrawImGuiPortalWindow();
            DrawImGUIColourCorrectionWindow();
            DrawLightResolveImGuiWindow();
            DrawImGuiBloomWindow();
            DrawImGuiSSAOWindow();
            RenderBloomImGuiWindow();
            queryPool.DrawWindow(nameof(queryPool));
            CPUProfiler.Render(CPUFrame);
            GPUProfiler.Render();
            EventProfiler.DrawImGuiWindow();
            TaskMaster.DrawImGuiWindow();

            ImGuiController?.Render();
        }

        [Conditional("DEBUG")]
        private void DrawImGuiOptionsWindow()
        {
            if (!ImGui.Begin("Options"))
                return;

            ImGui.Checkbox("FXAA", ref enableFXAA);
            ImGui.Checkbox("SSAO", ref enableSSAO);
            ImGui.Checkbox("Bloom", ref enableBloom);
            ImGui.Checkbox("Update Visibility", ref shouldUpdateVisibility);

            ImGui.End();
        }

        [Conditional("DEBUG")]
        [Conditional("BETA")]
        [Conditional("PROFILE")]
        private void DrawImGuiMetricsWindow()
        {
            if (!ImGui.Begin("Metrics"))
                return;

            const int TargetFPS = 144;
            var values = GPUFrameTimings.ToArray();
            float average = values.Average();
            var red = (float)MathFunctions.Map(average, 144, 120, 0, 1);
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new System.Numerics.Vector4(red, 1 - red, 0, 1));
            ImGui.PlotHistogram("GPU", ref values[0], values.Length, 0, null, 144 * 2, 60, new System.Numerics.Vector2(CPUFrameTimings.Count * 2, 50));
            ImGui.Text($"Average: {1000f / average:0.00}ms ({average:0.000} fps)");
            ImGui.PopStyleColor();

            values = CPUFrameTimings.ToArray();
            average = values.Average();
            ImGui.PlotHistogram("CPU", ref values[0], values.Length, 0, null, 0, 1000f / TargetFPS, new System.Numerics.Vector2(CPUFrameTimings.Count * 2, 50));
            ImGui.Text($"Average: {average:0.000}ms ({1000f / average:00.00} fps)");

            Metrics.AddImGuiMetrics();

            ImGui.End();
        }

        [Conditional("DEBUG")]
        private void DrawImGuiMaterialWindow()
        {
            if (!ImGui.Begin("Deferred Rendering Material"))
                return;

            ImGui.SliderFloat(nameof(DiffuseScalar), ref DiffuseScalar, 0.0f, 2.0f);
            ImGui.SliderFloat(nameof(LightBrightnessMultiplier), ref LightBrightnessMultiplier, 0.0f, 128.0f);
            ImGui.Checkbox(nameof(UseLightDiffuse), ref UseLightDiffuse);
            ImGui.Separator();
            ImGui.SliderFloat(nameof(SpecularScalar), ref SpecularScalar, 0.0f, 2.0f);
            ImGui.SliderFloat(nameof(SpecularPowerScalar), ref SpecularPowerScalar, 0.0f, 32.0f);
            ImGui.SliderFloat(nameof(OffsetByNormalScalar), ref OffsetByNormalScalar, 0.0f, 0.5f);
            ImGui.Checkbox(nameof(UseLightSpecular), ref UseLightSpecular);
            ImGui.Separator();
            ImGui.SliderFloat(nameof(LightScatterScalar), ref LightScatterScalar, 0.0f, 2.0f);
            ImGui.NewLine();
            ImGui.NewLine();
            ImGui.SliderFloat(nameof(LightingScalar), ref LightingScalar, 1.0f, 16.0f);

            ImGui.End();
        }

        [Conditional("DEBUG")]
        private void DrawImGuiPortalWindow()
        {
            if (!ImGui.Begin("Portals"))
                return;

            ImGui.Checkbox("Enable", ref enablePortalCulling);
            ImGui.Text("Visible Areas");
            foreach (var area in VisibleAreas)
                ImGui.Text(area.Name);
            ImGui.Separator();
            ImGui.Text("Active Portal Queries");
            foreach (var portal in PortalQueries)
                ImGui.Text(portal.Item1.Name);

            ImGui.End();
        }

        [Conditional("DEBUG")]
        private void DrawImGUIColourCorrectionWindow()
        {
            if (!ImGui.Begin("Colour Correction"))
                return;

            ImGui.DragFloat("Key", ref Key, 0.01f, 0.01f, 2.0f);
            ImGui.DragFloat("Exposure", ref Exposure, 0.01f, 0.5f, 2.0f);
            ImGui.DragFloat("Gamma", ref Gamma, 0.01f, 1.0f, 2.5f);
            ImGui.DragFloat("White cut", ref WhiteCut, 0.01f, 0.1f, 10.0f);

            ImGui.End();
        }

        [Conditional("DEBUG")]
        private void DrawLightResolveImGuiWindow()
        {
            if (!enableImGui)
                return;

            if (ImGui.Begin("Light Resolve"))
            {
                ImGui.SliderFloat("Noise scale", ref NoiseScalar, 0f, 0.25f);
            }
            ImGui.End();
        }

        [Conditional("DEBUG")]
        private void DrawImGuiBloomWindow()
        {
            if (!ImGui.Begin("Bloom"))
                return;

            ImGui.DragFloat("Bright pass", ref BrightPass, 0.01f, 0.01f, 20f);
            ImGui.DragFloat("Bright scaler", ref BrightScalar, 0.01f, 0.00f, 100f);
            ImGui.Separator();
            ImGui.SliderFloat3("Size Weight", ref SizeWeight, 0f, 1f);
            ImGui.DragFloat("Weight Scalar", ref WeightScalar, 0.01f, 0, 10f);
            ImGui.Separator();

            ImGui.End();
        }

        [Conditional("DEBUG")]
        private void DrawImGuiSSAOWindow()
        {
            if (!ImGui.Begin("SSAO"))
                return;

            ImGui.DragInt("Min Samples", ref MinSSAOSamples, 1, 1, MaxSSAOSamples);
            ImGui.DragInt("Max Samples", ref MaxSSAOSamples, 1, MinSSAOSamples, 64);
            ImGui.DragFloat("Max Samples Distance", ref MaxSamplesDistance, 0.1f, 0, 10);
            ImGui.DragFloat("Intensity", ref Intensity, 0.01f, 0.01f, 8f * 8);
            ImGui.DragFloat("Bias", ref Bias, 0.01f, 0.01f, 0.5f);
            ImGui.DragFloat("Sample Radius", ref SampleRadius, 0.001f, 0.001f, 0.2f);
            ImGui.DragFloat("Sample Disatance", ref MaxDistance, 0.0f, 0.1f, 1);

            ImGui.End();
        }

        [Conditional("DEBUG")]
        private void RenderBloomImGuiWindow()
        {
            if (ImGui.Begin("Blur"))
            {
                ImGui.SliderInt("NumSamples", ref NumBlurSamples, 6, 32);
                ImGui.SliderFloat("Horizontal Width", ref BlurWidthPercent, 1, 25);
                ImGui.SliderFloat("Offset", ref BlurPixelOffset, 0, 0.75f);
                ImGui.SliderFloat("Dirt Highlights", ref DirtHighlightScalar, 0, 5);
                ImGui.SliderFloat("Dirt General", ref DirtGeneralScalar, 0, 0.1f);
                ImGui.SliderFloat("Crack Strength", ref CrackScalar, 0, 0.1f);
            }
        }

        #endregion

        #region Debug Rendering

        [Conditional("DEBUG")]
        private void RenderSkeletons()
        {
            if (showSkeletons)
            {
                foreach (var skinnedModel in scene.Models.Where(model => model.IsSkinned).Cast<AnimatedModel>())
                {
                    var modelspaceTransforms = skinnedModel.ModelSpaceBoneTransforms;
                    skinnedModel.Skeleton.Render(
                    LineRenderer,
                        modelspaceTransforms,
                        skinnedModel.Transform.Matrix
                    );
                }
            }

            LineRenderer.AddAxisHelper(Matrix4.Identity);
        }

        [Conditional("DEBUG")]
        private void RenderBoundingBoxes()
        {
            GL.Disable(EnableCap.DepthTest);
            LineRenderer.Render();
            GL.Enable(EnableCap.DepthTest);

            // TODO: Bounding boxes should use LineRenderer now
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
                foreach (var terrainPeice in scene.Terrain)
                    terrainPeice.RenderBoundingBox();

                GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
            }
        }
       
#endregion

#region Events

        private void OnBenchmarkComplete()
        {
            Close();
        }
        
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            //ImGuiController.WindowResized(ClientSize.X, ClientSize.Y);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            Camera.Update(KeyboardState);

            //CSV.Append(Camera.Position.X + "," + Camera.Position.Y + "," + Camera.Position.Z + ",");
            //CSV.AppendLine(Camera.Rotation.X + "," + Camera.Rotation.Y);

            if (IsFocused)
                UpdateDebugVars();
        }

        [Conditional("DEBUG")]
        [Conditional("BETA")]
        [Conditional("PROFILE")]
        private void UpdateDebugVars()
        {
            var input = KeyboardState;

            if (input.IsKeyPressed(Keys.L))
                debugLights = !debugLights;

            if (input.IsKeyDown(Keys.X))
                HPLModelLoader.Offset.X += 0.01f;
            if (input.IsKeyDown(Keys.Y))
                HPLModelLoader.Offset.Y += 0.01f;
            if (input.IsKeyDown(Keys.Z))
                HPLModelLoader.Offset.Z += 0.01f;

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

            if (input.IsKeyPressed(Keys.V))
                VSync = VSync == VSyncMode.Off ? VSyncMode.On : VSyncMode.Off;

            if (input.IsKeyPressed(Keys.B))
                showBoundingBoxes = !showBoundingBoxes;

            if (input.IsKeyPressed(Keys.G))
                shouldUpdateVisibility = !shouldUpdateVisibility;

            if (input.IsKeyPressed(Keys.F1))
            {
                enableImGui = !enableImGui;
                bindMouse = !enableImGui;
            }
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);

            ImGuiController?.PressChar((char)e.Unicode);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            ImGuiController?.MouseScroll(e.Offset);
        }

        protected override void OnClosing(CancelEventArgs e) {
            Mouse.Grabbed = false;

            //File.WriteAllText("Recording.csv", CSV.ToString());

            Metrics.StopRecording();

            base.OnClosing(e);
        }

#endregion
    }
}
