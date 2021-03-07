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
        private Scene scene;

        private FrameBuffer GBuffers;
        private FrameBuffer LightingBuffer;
        private FrameBuffer[] BloomBuffers;
        private FrameBuffer StagingBuffer1, StagingBuffer2;
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
        private QueryPool queryPool;
        private bool DebugLights;

        private int debugGBufferTexture = -1;
        private bool debugLightBuffer;
        private bool useFXAA = false;
        private bool useSSAO = false;
        private bool showBoundingBoxes = false;

        private Buffer<Matrix4> cameraBuffer;
        private Buffer<SpotLight> spotLightsBuffer;
        private Buffer<PointLight> pointLightsBuffer;
        private Buffer<DeferredGeoMaterial> materialBuffer;
        private Buffer<Matrix4> matrixBuffer;
        private Buffer<DrawElementsIndirectData> drawIndirectBuffer;
        private Matrix4[] ModelMatricies;

        private Query GeoPassQuery;

        private const int maxLights = 500;
        private int bloomDataStride = 1000;
        private float elapsedMilliseconds = 0;
        private Buffer<float> bloomBuffer;
        private readonly DateTime startTime = DateTime.Now;

        private List<QueryPair> GeoStageQueries = new List<QueryPair>();

        private readonly Vector3
            CustomMapCameraPosition = new Vector3(6.3353596f, 1.6000088f, 8.1601305f),
            PhiMapCameraPosition = new Vector3(-17.039896f, 14.750014f, 64.48185f);

        public HPL(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings) {
            Camera = new DebugCamera(PhiMapCameraPosition, new Vector3(), 90);
            Camera.Width = Width;
            Camera.Height = Height;
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
                    PixelInternalFormat.Srgb8Alpha8,// Diffuse
                    PixelInternalFormat.Rgb16f,     // Position
                    PixelInternalFormat.Rgb8,       // Normal
                    PixelInternalFormat.Srgb8Alpha8,// Specular
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
            StagingBuffer1 = new FrameBuffer(Width, Height, false, PixelInternalFormat.Rgb16f);
            StagingBuffer2 = new FrameBuffer(Width, Height, false, PixelInternalFormat.Rgb16f);

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
            queryPool = new QueryPool(5);

            var lab = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter00\00_03_laboratory\00_03_laboratory.hpm";
            var bedroom = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter00\00_01_apartment\00_01_apartment.hpm";
            var theta_outside = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter02\02_04_theta_outside\02_04_theta_outside.hpm";
            var upsilon = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter01\01_02_upsilon_inside\01_02_upsilon_inside.hpm";
            var theta_inside = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter02\02_05_theta_inside\02_05_theta_inside.hpm";
            var tau = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter04\04_02_tau_inside\04_02_tau_inside.hpm";
            var phi = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\chapter05\05_01_phi_inside\05_01_phi_inside.hpm";
            var custom = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\custom\custom.hpm";
            var boundingBoxes = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\maps\Testing\BoundingBoxes\BoundingBoxes.hpm";
            var mapToLoad = phi;
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
            var map = new Map(
                mapToLoad,
                assimp,
                deferredMaterial
            );
            scene = map.ToScene();
            var afterMapLoad = DateTime.Now;
            var sorter = new DeferredMaterialGrouper();
            var beforeMapSort = DateTime.Now;
            var batches = GroupBy(scene.Models, (a, b) =>
            {
                var mat1 = (DeferredRenderingGeoMaterial)a.Material;
                var mat2 = (DeferredRenderingGeoMaterial)b.Material;
                return a.VAO.container.Handle == b.VAO.container.Handle
                && mat1.DiffuseTexture == mat2.DiffuseTexture
                && mat1.SpecularTexture == mat2.SpecularTexture
                && mat1.NormalTexture == mat2.NormalTexture
                && mat1.IlluminationColor == mat2.IlluminationColor;
            }).OrderBy(b => b.Models[0].Material.Shader.Handle);
            scene.Batches = batches.ToList();
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
            var allModels = map.Entities.Sum(e => e.Models.Count);
            Console.WriteLine($"Scene: {numStatic} Static, {numStaticOccluders} of which occlude. {numDynamic} Dynamic. {allModels} Total.");

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

        public List<RenderBatch<DeferredRenderingGeoMaterial>> GroupBy(IEnumerable<Model> models, Func<Model, Model, bool> comparer)
        {
            var batches = new List<RenderBatch<DeferredRenderingGeoMaterial>>();

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
                    batches.Add(new RenderBatch<DeferredRenderingGeoMaterial>(new[] { model }));
            }

            return batches;
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

            //map.Render(projectionMatrix, viewMatrix);

            if (GeoPassQuery != null)
            {
                var time = GeoPassQuery.GetResult();
                //Console.WriteLine(time / 1000000f + "ms");
            }

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

            using (GeoPassQuery = queryPool.BeginScope(QueryTarget.TimeElapsed))
            {
                MultiDrawIndirect();
            }

            FinishDeferredRendering(projectionMatrix, viewMatrix);

            if (showBoundingBoxes)
                scene.RenderBoundingBoxes(Camera.ProjectionMatrix, Camera.ViewMatrix);
            
            SwapBuffers();
            NewFrame();
            elapsedMilliseconds = (float)(DateTime.Now - startTime).TotalMilliseconds;
            Title = FPS.ToString() + "FPS";
        }

        private void MultiDrawIndirect()
        {
            //updateModelMatriciesBuffer();

            var drawCommandPtr = (IntPtr)0;
            var modelMatrixPtr = 0;
            var materialPtr = 0;
            var commandSize = Marshal.SizeOf<DrawElementsIndirectData>();
            var matrixSize = Marshal.SizeOf<Matrix4>();
            var materialSize = Marshal.SizeOf<DeferredGeoMaterial>();

            // TODO: Possibly put model matricies into a UBO. To do this the batchSize below would need a maximum to fit modelMatricies into uniform buffer.
            //GL.BindBuffer(BufferTarget.ShaderStorageBuffer, modelMatriciesSSBO);

            GeoStageQueries.Clear();
            Query runningQuery = null;
            int i = 0;
            foreach (var batch in scene.Batches)
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
                        shader =  (DeferredRenderingGeoShader)Shader.Current
                    });
                }

                matrixBuffer.BindRange(modelMatrixPtr, 1, batchSize * matrixSize);
                materialBuffer.BindRange(materialPtr, 2, batchSize * materialSize);
                GL.MultiDrawElementsIndirect(
                    OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles,
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

        private void setupBuffers()
        {
            setupCameraUniformBuffer();
            setupLightingUBO();
            updatePointLightsUBO();
            updateSpotLightsUBO();

            setupBloomUBO();

            var batches = scene.Batches;
            //var batchSizes = scene.Batches.Select(b => b.Models.Count()).ToArray();
            //var numModels = batchSizes.Sum();
            //setupMaterialBuffer(models, numModels);
            //setupModelMatriciesBuffer(batchSizes);
            //setupDrawIndirectBuffer(models, numModels);
            loadFrameData(batches);

            setupRandomTexture();
        }

        private void loadFrameData(List<RenderBatch<DeferredRenderingGeoMaterial>> batches)
        {
            var sizeOfMatrix = Marshal.SizeOf<Matrix4>();

            var modelMatricies = new List<Matrix4>();
            var drawCommands = new List<DrawElementsIndirectData>();
            var materials = new List<DeferredGeoMaterial>();
            foreach (var batch in batches)
            {
                uint i = 0;
                foreach (var model in batch.Models)
                {
                    modelMatricies.Add(MathFunctions.CreateModelMatrix(model.Transform.Position, model.Transform.Rotation, model.Transform.Scale));

                    var mat = (DeferredRenderingGeoMaterial)model.Material;
                    materials.Add(new DeferredGeoMaterial()
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
                        materials.Add(new DeferredGeoMaterial());
                        drawCommands.Add(new DrawElementsIndirectData());
                    }
                }
            }

            ModelMatricies = modelMatricies.ToArray();
            matrixBuffer = new Buffer<Matrix4>(ModelMatricies, BufferTarget.UniformBuffer, BufferUsageHint.StreamDraw, "ModelMatricies");
            matrixBuffer.BindRange(0, 1);

            var flattenedDrawCommands = drawCommands.ToArray();
            drawIndirectBuffer = new Buffer<DrawElementsIndirectData>(flattenedDrawCommands, BufferTarget.DrawIndirectBuffer, BufferUsageHint.StaticDraw, "DrawCommands");

            var flattenedMaterials = materials.ToArray();
            materialBuffer = new Buffer<DeferredGeoMaterial>(flattenedMaterials, BufferTarget.ShaderStorageBuffer, BufferUsageHint.StaticDraw, "MaterialData");
            materialBuffer.BindRange(0, 2);
        }

        [StructLayout(LayoutKind.Explicit, Size = 64)]
        struct DeferredGeoMaterial
        {
            [FieldOffset(00)] public Vector3 IlluminationColor;
            [FieldOffset(16)] public Vector3 AlbedoColourTint;
            [FieldOffset(32)] public Vector2 TextureRepeat;
            [FieldOffset(40)] public Vector2 TextureOffset;
            [FieldOffset(48)] public float NormalStrength;
            [FieldOffset(52)] public bool IsWorldSpaceUVs;
        }

        private void setupCameraUniformBuffer()
        {
            cameraBuffer = new Buffer<Matrix4>(3, BufferTarget.UniformBuffer, BufferUsageHint.StreamRead, "CameraData");
            cameraBuffer.BindRange(0, 0);
        }

        private void setupLightingUBO()
        {
            pointLightsBuffer = new Buffer<PointLight>(Math.Min(maxLights, scene.PointLights.Count), BufferTarget.UniformBuffer, BufferUsageHint.StaticDraw, "PointLights");
            pointLightsBuffer.BindRange(0, 3);
            spotLightsBuffer = new Buffer<SpotLight>(Math.Min(maxLights, scene.SpotLights.Count), BufferTarget.UniformBuffer, BufferUsageHint.StaticDraw, "SpotLights");
            spotLightsBuffer.BindRange(0, 4);
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

        private void updateCameraUBO()
        {
            var projectionMatrix = Camera.ProjectionMatrix;
            var viewMatrix = Camera.ViewMatrix;
            var projectionView = new Matrix4();
            MatrixExtensions.Multiply(projectionMatrix, viewMatrix, ref projectionView);
            var data = new[]
            {
                projectionMatrix, viewMatrix, projectionView
            };

            cameraBuffer.Update(data);
        }

        private void updatePointLightsUBO()
        {
            var lights = new PointLight[Math.Min(maxLights, scene.PointLights.Count)];
            for (var i = 0; i < lights.Length; i++)
            {
                var pointLight = scene.PointLights[i];
                pointLight.GetLightingScalars(out float diffuseScalar, out float specularScalar);
                lights[i] = new PointLight(
                    pointLight.Position,
                    pointLight.Color,
                    pointLight.Brightness,
                    pointLight.Radius,
                    pointLight.FalloffPower,
                    diffuseScalar,
                    specularScalar
                );
            }

            pointLightsBuffer.Update(lights);
        }

        private void updateSpotLightsUBO()
        {
            var lights = new SpotLight[Math.Min(maxLights, scene.SpotLights.Count)];
            for (var i = 0; i < lights.Length; i++)
            {
                var spotLight = scene.SpotLights[i];
                spotLight.GetLightingScalars(out float diffuseScalar, out float specularScalar);
                var dir = spotLight.Rotation * new Vector3(0, 0, 1);
                lights[i] = new SpotLight(
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

            spotLightsBuffer.Update(lights);
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
                DoLightPass(projectionMatrix, viewMatrix, new Vector3(0.00f));

                if (debugLightBuffer) {
                    GL.Enable(EnableCap.FramebufferSrgb);
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                    GL.ClearColor(0, 0, 0, 1);
                    GL.Clear(ClearBufferMask.ColorBufferBit);

                    var shader = FullBrightShader;
                    shader.Use();
                    LightingBuffer.ColorBuffers[0].Use(TextureUnit.Texture0);
                    shader.Set("texture0", TextureUnit.Texture0);
                    Primitives.Quad.Draw();

                    GL.Disable(EnableCap.FramebufferSrgb);
                } else {
                    StagingBuffer1.Use();
                    GL.ClearColor(0, 0, 0, 1);
                    GL.Clear(ClearBufferMask.ColorBufferBit);

                    var shader = FinalCombineShader;
                    shader.Use();
                    LightingBuffer.ColorBuffers[0].Use(TextureUnit.Texture0);
                    GBuffers.ColorBuffers[0].Use(TextureUnit.Texture1);
                    shader.Set("texture0", TextureUnit.Texture0);
                    shader.Set("texture1", TextureUnit.Texture1);
                    Primitives.Quad.Draw();

                    DoBloomPass();

                    StagingBuffer2.Use();
                    shader = ColorCorrectionShader;
                    shader.Use();
                    StagingBuffer1.ColorBuffers[0].Use(TextureUnit.Texture0);
                    shader.Set("Texture", TextureUnit.Texture0);
                    Primitives.Quad.Draw();

                    if (useFXAA)
                    {
                        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
                        shader = FXAAShader;
                        shader.Use();
                        StagingBuffer2.ColorBuffers[0].Use(TextureUnit.Texture0);
                        shader.Set("Texture", TextureUnit.Texture0);
                        Primitives.Quad.Draw();

                    } 
                    else
                    {
                        // Blit to default frame buffer
                        StagingBuffer2.BlitTo(0, Width, Height, ClearBufferMask.ColorBufferBit);
                    }
                }
            }

            // Blit to default frame buffer
            GBuffers.BlitTo(0, Width, Height, ClearBufferMask.DepthBufferBit);

            //GL.DepthMask(true);
            GL.Enable(EnableCap.DepthTest);
        }

        private void DoBloomPass()
        {
            // Extract bright parts
            StagingBuffer2.Use();
            ExtractBright.Use();
            StagingBuffer1.ColorBuffers[0].Use(TextureUnit.Texture0);
            ExtractBright.Set("diffuseMap", TextureUnit.Texture0);
            ExtractBright.Set("avInvScreenSize", new Vector2(1f / Width, 1f / Height));
            ExtractBright.Set("afBrightPass", 15f);
            Primitives.Quad.Draw();

            // Take bright parts and blur
            bloomBuffer.Bind();

            int width = Width,
                height = Height;
            var previousTexture = StagingBuffer2.ColorBuffers[0];
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

                    previousTexture.Use(TextureUnit.Texture0);
                    shader.Set("diffuseMap", TextureUnit.Texture0);

                    Primitives.Quad.Draw();

                    previousTexture = BloomBuffers[i].ColorBuffers[0];
                    i++;
                }
            }

            // Add all to finished frame
            GL.Viewport(0, 0, Width, Height);
            StagingBuffer1.Use();
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);

            shader = BloomCombineShader;
            shader.Use();
            Texture.Use(new[] { BloomBuffers[1].ColorBuffers[0], BloomBuffers[3].ColorBuffers[0], BloomBuffers[5].ColorBuffers[0], NoiseMap }, TextureUnit.Texture0);
            shader.Set("blurMap0", TextureUnit.Texture0);
            shader.Set("blurMap1", TextureUnit.Texture1);
            shader.Set("blurMap2", TextureUnit.Texture2);
            shader.Set("noiseMap", TextureUnit.Texture3);
            shader.Set("avInvScreenSize", new Vector2(1f / Width, 1f / Height));
            shader.Set("timeMilliseconds", elapsedMilliseconds);
            Primitives.Quad.Draw();

            GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
            GL.Disable(EnableCap.FramebufferSrgb);
        }

        private void DisplayGBuffer(int buffer)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Enable(EnableCap.FramebufferSrgb);

            var shader = FullBrightShader;
            shader.Use();
            GBuffers.ColorBuffers[buffer].Use(TextureUnit.Texture0);
            shader.Set("texture0", 0);
            Primitives.Quad.Draw();

            GL.Disable(EnableCap.FramebufferSrgb);
        }

        public void DoLightPass(Matrix4 projectionMatrix, Matrix4 viewMatrix, Vector3 ambientColor)
        {
            LightingBuffer.Use();
            GL.ClearColor(ambientColor.X, ambientColor.Y, ambientColor.Z, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
            GL.CullFace(CullFaceMode.Front);

            RenderLights(projectionMatrix, viewMatrix);

            GL.CullFace(CullFaceMode.Back);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
            GL.BlendEquation(BlendEquationMode.FuncReverseSubtract);

            if (useSSAO)
                SSAOPostEffect();

            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
        }

        private void RenderLights(Matrix4 projectionMatrix, Matrix4 viewMatrix)
        {
            if (scene.PointLights.Any())
            {
                pointLightsBuffer.BindRange(0, 1);
                var shader = PointLightShader;
                shader.Use();
                Texture.Use(new[] {
                    GBuffers.ColorBuffers[(int)GBufferTexture.Diffuse],
                    GBuffers.ColorBuffers[(int)GBufferTexture.Position],
                    GBuffers.ColorBuffers[(int)GBufferTexture.Normal],
                    GBuffers.ColorBuffers[(int)GBufferTexture.Specular],
                }, TextureUnit.Texture0);
                shader.Set("diffuseTex", TextureUnit.Texture0);
                shader.Set("positionTex", TextureUnit.Texture1);
                shader.Set("normalTex", TextureUnit.Texture2);
                shader.Set("specularTex", TextureUnit.Texture3);
                shader.Set("camPos", Camera.Position);
                //TODO: Could render a 2D circle in screenspace instead of a sphere

                var numPointLights = Math.Min(maxLights, scene.PointLights.Count);

                Primitives.Sphere.Draw(numInstances: numPointLights);

                // Debug light spheres
                if (DebugLights)
                {
                    for (var i = 0; i < scene.PointLights.Count; i++)
                    {
                        var light = scene.PointLights[i];
                        var modelMatrix = MathFunctions.CreateModelMatrix(light.Position, new OpenTK.Mathematics.Quaternion(), new Vector3(light.Radius * 2));
                        singleColorMaterial.ProjectionMatrix = projectionMatrix;
                        singleColorMaterial.ViewMatrix = viewMatrix;
                        singleColorMaterial.ModelMatrix = modelMatrix;
                        singleColorMaterial.Commit();
                        Primitives.Sphere.Draw(OpenTK.Graphics.OpenGL4.PrimitiveType.Lines);
                    }
                }
            }

            if (scene.SpotLights.Any())
            {
                spotLightsBuffer.BindRange(0, 1);
                var numSpotLights = Math.Min(maxLights, scene.SpotLights.Count);

                Shader shader = SpotLightShader;
                shader.Use();
                Texture.Use(new[] {
                    GBuffers.ColorBuffers[(int)GBufferTexture.Diffuse],
                    GBuffers.ColorBuffers[(int)GBufferTexture.Position],
                    GBuffers.ColorBuffers[(int)GBufferTexture.Normal],
                    GBuffers.ColorBuffers[(int)GBufferTexture.Specular],
                }, TextureUnit.Texture0);
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
                        var light = scene.SpotLights[i];
                        var modelMatrix = MathFunctions.CreateModelMatrix(light.Position, new OpenTK.Mathematics.Quaternion(), new Vector3(light.Radius * 2));
                        singleColorMaterial.ModelMatrix = modelMatrix;
                        singleColorMaterial.ProjectionMatrix = projectionMatrix;
                        singleColorMaterial.ViewMatrix = viewMatrix;
                        singleColorMaterial.Commit();
                        Primitives.Sphere.Draw(OpenTK.Graphics.OpenGL4.PrimitiveType.Lines);
                    }
                }
            }
        }

        private void SSAOPostEffect()
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

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            Camera.Update(KeyboardState);

            if (IsFocused) {
                var input = KeyboardState;

                if (KeyboardState.IsKeyPressed(Keys.L))
                    DebugLights = !DebugLights;

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
            Specular
        }
        private struct QueryPair
        {
            public Query Query;
            public DeferredRenderingGeoShader shader;
        }
    }
}
