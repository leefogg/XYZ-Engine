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
        private Skeleton skeleton;
        private Texture2D AlbedoTexture;

        public AnimationTest(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            Camera = new Camera(new Vector3(-0.97707474f, 0.70000046f, 2.388693f), new Vector3(), 90)
            {
                CameraController = new PCCameraController()
            };

            ImGuiController = new ImGuiController(ClientSize.X, ClientSize.Y);
            LineRenderer = new DebugLineRenderer(1024);
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            OnLoad1();
        }

        private void OnLoad1()
        {
            var assimp = new Assimp.AssimpContext();
            var geo = new Geometry();
            List<Assimp.Bone> bones;
            Assimp.Node assimpRootNode;
            List<Assimp.Animation> baseAnimations;
            {
                var path = "assets/models/leviathan.dae";
                var steps = Assimp.PostProcessSteps.FlipUVs
                    | Assimp.PostProcessSteps.Triangulate
                    | Assimp.PostProcessSteps.ValidateDataStructure;
                var scene = assimp.ImportFile(path, steps);
                var mesh = scene.Meshes[0];
                bones = mesh.Bones;
                geo.Positions = mesh.Vertices.Select(v => v.ToOpenTK()).ToList();
                geo.UVs = mesh.TextureCoordinateChannels[0].Select(uv => new Vector2(uv.X, uv.Y)).ToList();
                geo.Indicies = mesh.GetUnsignedIndices().ToList();
                geo.Normals = mesh.Normals.Select(n => n.ToOpenTK()).ToList();

                //bones = mesh.Bones;
                var boneNames = mesh.Bones.Select(b => b.Name).ToArray();
                assimpRootNode = scene.RootNode.Find(n => boneNames.Contains(n.Name));
                baseAnimations = scene.Animations;
            }
            {
                var shader = new FullbrightShader();
                var material = new FullbrightMaterial(shader);
                var path = "assets/animations/attack.dae";
                var steps = Assimp.PostProcessSteps.LimitBoneWeights;
                var scene = assimp.ImportFile(path, steps);
                {
                    var mesh = scene.Meshes[0];
                    // Add animations
                    //skeleton = new Skeleton(assimpRootNode, bones, scene.Animations);
                    skeleton = new Skeleton(assimpRootNode, bones, scene.Animations.ToList());
                    skeleton.MergeAnims("Merged anim");
                }
            }

            var pairs = CreateVertexWeights(bones, geo.Positions.Count);
            var ids = pairs.SelectMany(p => p.Item1).ToVec4s();
            var weights = pairs.SelectMany(p => p.Item2).ToVec4s();

            geo.BoneIds = ids.ToList();
            geo.BoneWeights = weights.ToList();

            AlbedoTexture = TextureCache.Get("assets/textures/leviathan.png");
            SkinnedMesh = geo.ToVirtualVAO();
            SkeletonShader = new DynamicPixelShader(
                "assets/shaders/AnimatedModel/VertexShader.vert",
                "assets/shaders/AnimatedModel/FragmentShader.frag",
                null,
                "BoneWeights"
            );

            BonePosesUBO = new Buffer<Matrix4>(128, BufferTarget.UniformBuffer, BufferUsageHint.DynamicDraw, "Bone Poses");
            BonePosesUBO.Bind(2);
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

            //LineRenderer.DrawPlane(100, 100, 100, 100);

            //RenderOtherTest();
            RenderNormalTest();

            GL.Disable(EnableCap.DepthTest);
            //LineRenderer.Render();
            GL.Enable(EnableCap.DepthTest);
        }

        private void RenderNormalTest()
        {
            float timeMs = (float)GameMillisecondsElapsed;
            var modelSpaceTransforms = new Matrix4[skeleton.TotalBones];
            var boneSpaceTransforms = new Matrix4[skeleton.TotalBones];
            skeleton.GetModelSpaceTransforms(skeleton.Animations[0], timeMs, modelSpaceTransforms);
            skeleton.GetBoneSpaceTransforms(modelSpaceTransforms, boneSpaceTransforms);
            BonePosesUBO.Update(boneSpaceTransforms);

            var rotation = 00;
            var modelMatrix = skeleton.ModelMatrix * MathFunctions.CreateModelMatrix(
                Vector3.Zero,
                new Quaternion(0, 0, rotation * 0.0174533f), 
                new Vector3(1f)
            );
            SkeletonShader.Use();
            SkeletonShader.Set("ModelMatrix", modelMatrix);
            AlbedoTexture.Use();
            SkinnedMesh.Draw();

            skeleton.Render(LineRenderer, modelSpaceTransforms, modelMatrix);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            Camera.Update(KeyboardState);
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
