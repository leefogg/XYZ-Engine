using GLOOP;
using GLOOP.Animation;
using GLOOP.Extensions;
using GLOOP.Rendering;
using GLOOP.Rendering.Debugging;
using GLOOP.Rendering.Materials;
using GLOOP.Util;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AnimationTest
{
    internal class AnimationTest : Window
    {
        private readonly ImGuiController ImGuiController;
        private readonly DebugLineRenderer LineRenderer;
        private Camera Camera;
        private VirtualVAO SkinnedMesh;
        private DynamicPixelShader SkeletonShader;
        private Buffer<Matrix4> BonePosesUBO;
        private Skeleton Skeleton;
        private IList<SkeletonAnimation> Animations;
        private Texture2D AlbedoTexture;
        int animationId = 0;
        private ModelLoader LoadedEnt;

        public AnimationTest(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            Camera = new Camera(new Vector3(0,0,0), new Vector3(), 90)
            {
                CameraController = new PCCameraController()
            };

            ImGuiController = new ImGuiController(ClientSize.X, ClientSize.Y);
            LineRenderer = new DebugLineRenderer(128);
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            OnLoad1();
            //Benchmark();
        }

        private void OnLoad1()
        {

            var assimp = new Assimp.AssimpContext();
            var geo = new Geometry();
            List<Assimp.Bone> assimpBones;
            Assimp.Node assimpRootNode;
            List<Assimp.Animation> baseAnimations;
            var modelPath = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\entities\character\robot\spider\spider.dae";
            var animPath = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA\entities\character\robot\spider\animations\idle.dae_anim";
            AlbedoTexture = TextureCache.Get(@"C:\SOMA Resources\png\spider.png");

            LoadedEnt = new ModelLoader(modelPath, assimp, new SingleColorMaterial(new SingleColorShader3D()));
            {
                var steps = 
                    Assimp.PostProcessSteps.FlipUVs
                    | Assimp.PostProcessSteps.Triangulate
                    | Assimp.PostProcessSteps.CalculateTangentSpace
                    | Assimp.PostProcessSteps.GenerateNormals
                    | Assimp.PostProcessSteps.ValidateDataStructure;
                var assimpScene = assimp.ImportFile(modelPath, steps);
                var assimpMesh = assimpScene.Meshes[0];
                assimpBones = assimpMesh.Bones;
                geo.Positions = assimpMesh.Vertices.Select(v => v.ToOpenTK()).ToList();
                geo.UVs = assimpMesh.TextureCoordinateChannels[0].Select(uv => new Vector2(uv.X, uv.Y)).ToList();
                geo.Indicies = assimpMesh.GetUnsignedIndices().ToList();
                geo.Normals = assimpMesh.Normals.Select(n => n.ToOpenTK()).ToList();

                //bones = mesh.Bones;
                var boneNames = assimpMesh.Bones.Select(b => b.Name).ToArray();
                assimpRootNode = assimpScene.RootNode.Find(b => boneNames.Contains(b.Name));
                baseAnimations = assimpScene.Animations;
            }
            {
                var shader = new FullbrightShader();
                var material = new FullbrightMaterial(shader);

                var scene = assimp.ImportFile(animPath, Assimp.PostProcessSteps.LimitBoneWeights);
                {
                    // Add animations
                    //skeleton = new Skeleton(assimpRootNode, bones, scene.Animations);
                    Skeleton = new Skeleton(assimpRootNode, assimpBones);
                    Animations = new List<SkeletonAnimation>(4);
                    foreach (var anim in scene.Animations)
                    {
                        var mergedAnim = new SkeletonAnimation(
                            Animations.SelectMany(anim => anim.Bones).ToArray(),
                            "MergedAnim"
                        );
                        Animations.Clear();
                        Animations.Add(mergedAnim);
                    }
                }
            }

            var pairs = CreateVertexWeights(assimpBones, geo.Positions.Count);
            var ids = pairs.SelectMany(p => p.Item1).ToVec4s();
            var weights = pairs.SelectMany(p => p.Item2).ToVec4s();

            geo.BoneIds = ids.ToList();
            geo.BoneWeights = weights.ToList();


            SkinnedMesh = geo.ToVirtualVAO();


            SkeletonShader = new DynamicPixelShader(
                "assets/shaders/AnimatedModel/VertexShader.vert",
                "assets/shaders/AnimatedModel/FragmentShader.frag",
                null,
                "BoneWeights"
            );

            BonePosesUBO = new Buffer<Matrix4>(128, BufferTarget.UniformBuffer, BufferUsageHint.DynamicDraw, "Bone Poses");
            BonePosesUBO.Bind(Globals.BindingPoints.SSBOs.DeferredRendering.SkeletonBonePoses);
        }

        private void Benchmark()
        {
            int iterations = 100000;
            float progressStep = 1f / iterations;
            float progress = 0;
            var timer = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++, progress += progressStep)
            {
                var modelSpaceTransforms = new Matrix4[Skeleton.TotalBones];
                var boneSpaceTransforms = new Matrix4[Skeleton.TotalBones];
                Skeleton.GetModelSpaceTransforms(Animations[animationId], progressStep, modelSpaceTransforms);
                Skeleton.GetBoneSpaceTransforms(modelSpaceTransforms, boneSpaceTransforms);
            }
            timer.Stop();
            Console.WriteLine(timer.ElapsedMilliseconds / (float)iterations);
        }

        // TODO: Ids are ints not floats
        public (float[], float[], int)[] CreateVertexWeights(IEnumerable<Assimp.Bone> bones, int numVerts)
        {

            const float fillerValue = float.MaxValue;
            var vertcies = Enumerable.Range(0, numVerts)
                .Select(i => (
                    ids:     Enumerable.Repeat(fillerValue, 4).ToArray(),
                    weights: Enumerable.Repeat(fillerValue, 4).ToArray(),
                    index: i
                )
            ).ToArray();
            int boneIdx = 0;
            foreach (var bone in bones)
            {
                Debug.Assert(bone.HasVertexWeights);

                foreach (var weight in bone.VertexWeights)
                {
                    var nextBlankSlot = vertcies[weight.VertexID].ids.IndexOf(fillerValue);
                    vertcies[weight.VertexID].ids[nextBlankSlot] = boneIdx;
                    nextBlankSlot = vertcies[weight.VertexID].weights.IndexOf(fillerValue);
                    vertcies[weight.VertexID].weights[nextBlankSlot] = weight.Weight;
                }
                boneIdx++;
            }

            foreach (var vert in vertcies)
            {
                var sum = vert.weights.Where(x => x != fillerValue).Sum();
                ///Debug.Assert(sum > 0.99999f && sum < 1.0001);
                for (int i = 0; i < 4; i++)
                    if (vert.weights[i] != float.MaxValue)
                        Debug.Assert(vert.ids[i] != float.MaxValue, "weight linking to no bone");
            }

            // Cleanup
            foreach (var vert in vertcies)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (vert.ids[i] == fillerValue) vert.ids[i] = 0;
                    if (vert.weights[i] == fillerValue) vert.weights[i] = 0;
                }
            }

            return vertcies;
        }

        public override void Render()
        {
            updateCameraUBO(Camera.ProjectionMatrix, Camera.ViewMatrix);

            LineRenderer.AddAxisHelper(Matrix4.Identity);

            //LineRenderer.DrawPlane(1000, 1000, 10, 10);

            //RenderOtherTest();
            //RenderNormalTest();
            RenderNewTest();

            GL.Disable(EnableCap.DepthTest);
            LineRenderer.Render();
            GL.Enable(EnableCap.DepthTest);

            ImGuiController.Render();
        }

        private void RenderNewTest()
        {
            var model = LoadedEnt.Models[0] as AnimatedModel;
            //var boneSpaceTransforms = model.AnimationDriver.GetTransformsFor(Animations[animationId]).ToArray();
            //var modelSpaceTransforms = new Matrix4[Skeleton.TotalBones];
            //var boneSpaceTransforms = new Matrix4[Skeleton.TotalBones];
            //model.Skeleton.GetModelSpaceTransforms(model.Animations[1], (float)GameMillisecondsElapsed, modelSpaceTransforms);
            //model.Skeleton.GetBoneSpaceTransforms(modelSpaceTransforms, boneSpaceTransforms);
            //BonePosesUBO.Update(boneSpaceTransforms);
            //model.Skeleton.Render(LineRenderer, modelSpaceTransforms, LoadedEnt.Transform.Matrix);
            model.UpdateBoneTransforms();
            var modelspaceTransforms = model.ModelSpaceBoneTransforms;
            var bonespaceTransforms = model.BoneSpaceBoneTransforms;
            model.Skeleton.Render(LineRenderer, modelspaceTransforms, model.Transform.Matrix);
            BonePosesUBO.Update(bonespaceTransforms.ToArray());

            SkeletonShader.Use();
            SkeletonShader.Set("ModelMatrix", LoadedEnt.Transform.Matrix);
            AlbedoTexture.Use();
            SkinnedMesh.Draw();

            /*
            model.AnimationDriver.PrepareMatrixes(model.Animations[1]);
            model.Skeleton.Render(
                LineRenderer,
                SkeletonAnimationDriver.GetModelspaceTransforms(model.Skeleton.TotalBones),
                model.Transform.Matrix
            );
            var bonespaceTransforms = SkeletonAnimationDriver.GetBonespaceTransforms(model.Skeleton.TotalBones).ToArray();
            BonePosesUBO.Update(bonespaceTransforms);

            SkeletonShader.Use();
            SkeletonShader.Set("ModelMatrix", LoadedEnt.Transform.Matrix);
            AlbedoTexture.Use();
            SkinnedMesh.Draw();
            */
        }

        private void RenderNormalTest()
        {
            float animStartMs = 0;
            float? animEndMs = null;
            var timeMs = (float)GameMillisecondsElapsed;
            var animLength = (animEndMs ?? Animations[animationId].Bones[0].RotationKeyframes.LengthMs) - animStartMs;
            timeMs = animStartMs + (timeMs % animLength);

            //var modelSpaceTransforms = new Matrix4[Skeleton.TotalBones];
            //var boneSpaceTransforms = new Matrix4[Skeleton.TotalBones];
            //Skeleton.GetModelSpaceTransforms(Animations[animationId], timeMs, modelSpaceTransforms);
            //Skeleton.GetBoneSpaceTransforms(modelSpaceTransforms, boneSpaceTransforms);
            //var boneSpaceTransforms = Driver.GetBonespaceTransformsFor(Animations[animationId]).ToArray();
            //BonePosesUBO.Update(boneSpaceTransforms);

            var rotation = 00;
            var modelMatrix = Skeleton.ModelMatrix * MathFunctions.CreateModelMatrix(
                Vector3.Zero,
                new Quaternion(0, 0, rotation * 0.0174533f),
                new Vector3(1f)
            );
            SkeletonShader.Use();
            SkeletonShader.Set("ModelMatrix", modelMatrix);
            AlbedoTexture.Use();
            SkinnedMesh.Draw();

            //skeleton.Render(LineRenderer, modelSpaceTransforms, modelMatrix);

            renderImGuiWindow(animStartMs, animEndMs, timeMs, animLength);
        }

        private void renderImGuiWindow(float animStartMs, float? animEndMs, float timeMs, float animLength)
        {
            if (!ImGui.Begin("Timeline"))
                return;

            var bone = Animations[animationId].Bones[0];
            var numSamples = 1000;
            var samples = new float[numSamples];
            var lengthMs = bone.RotationKeyframes.LengthMs;
            var stepSize = lengthMs / numSamples;
            var sampleTime = 0f;
            for (int i = 0; i < numSamples; i++, sampleTime += stepSize)
                samples[i] = bone.RotationKeyframes.GetValueAtTime(sampleTime).Z;
            var chartSize = new System.Numerics.Vector2(1000, 100);

            var windowPos = ImGui.GetWindowPos();
            windowPos.Y += 25;
            windowPos.X += 10;
            ImGuiWidgets.AddTimeline(windowPos, samples, timeMs, animStartMs, animEndMs ?? animLength, lengthMs, chartSize);

            ImGui.End();
        }


        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            Camera.Update(KeyboardState);
            ImGuiController.Update(this, (float)args.Time);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Mouse.Grabbed = false;

            base.OnClosing(e);
        }

        public static void Main(string[] _)
        {
            const int screenSizeX = 2560;
            const int screenSizeY = 1440;
            const uint width = 1920;
            const uint height = 1080;

            var gameWindowSettings = new GameWindowSettings();
            var nativeWindowSettings = new NativeWindowSettings
            {
                API = ContextAPI.OpenGL,
                APIVersion = new Version(4, 3),
                Profile = ContextProfile.Core,
                Size = new Vector2i((int)width, (int)height),
            };
            nativeWindowSettings.Location = new Vector2i(
                screenSizeX / 2 - nativeWindowSettings.Size.X / 2,
                screenSizeY / 2 - nativeWindowSettings.Size.Y / 2
            );
            using var window = new AnimationTest(gameWindowSettings, nativeWindowSettings);
            window.VSync = VSyncMode.On;
            window.Run();
        }
    }
}
