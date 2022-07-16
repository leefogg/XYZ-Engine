﻿using GLOOP;
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
        private Model OrignalModel;
        private DynamicPixelShader SkeletonShader;
        private Buffer<Matrix4> BonePosesUBO;
        private Dictionary<string, Bone> AllBones;
        private Bone RootNode;
        private Skeleton skeleton;
        private Model Sphere;
        private Texture2D AlbedoTexture;
        private Bone TestSkeleton;

        public AnimationTest(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            Camera = new Camera(new Vector3(-3.7229528f, 2.800001f, 8.501869f), new Vector3(), 90)
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
            var shader = new FullbrightShader();
            var material = new FullbrightMaterial(shader);
            var path = "assets/models/model.dae";

            var steps = Assimp.PostProcessSteps.FlipUVs
                | Assimp.PostProcessSteps.GenerateNormals
                | Assimp.PostProcessSteps.CalculateTangentSpace
                | Assimp.PostProcessSteps.LimitBoneWeights
                | Assimp.PostProcessSteps.Triangulate;
            var scene = assimp.ImportFile(path, steps);

            var animations = scene.Animations[0];
            var assimpSkeleton = scene.RootNode.Children.First(child => child.Name == "Armature");
            var nodes = new List<Assimp.Node>();
            GetAll(nodes, assimpSkeleton);
            var rot = new DynamicTransform(nodes[1].Transform.ToOpenTK());
            nodes = nodes.Skip(1).ToList();

            AllBones = new Dictionary<string, Bone>();
            for (int i = 0; i < scene.Meshes[0].BoneCount; i++)
            {
                var bone = scene.Meshes[0].Bones[i];
                var node = nodes[i];

                var offsetFromParent = node.Transform.ToOpenTK();
                var modelToBone = bone.OffsetMatrix.ToOpenTK();

                var newBone = new Bone(bone.Name, i, offsetFromParent, modelToBone);

                var anim = animations.NodeAnimationChannels.FirstOrDefault(c => c.NodeName == bone.Name);
                if (anim != null)
                    newBone.AddAnimation(anim, (float)animations.TicksPerSecond);
                AllBones.Add(newBone.Name, newBone);
            }
            RootNode = CreateSkeleton(AllBones, assimpSkeleton.Children[0]);

            skeleton = new Skeleton(RootNode);
            skeleton.Animations.Add(new SkeletonAnimation(animations.NodeAnimationChannels, animations.Name, (float)animations.TicksPerSecond));

            for (int i = 0; i < nodes.Count; i++)
                Debug.Assert(AllBones[nodes[i].Name].Index == i, "Mismatched IDs");

            int j = 0;
            var jointOrder = new[] { "Torso", "Chest", "Neck", "Head", "Upper_Arm_L", "Lower_Arm_L", "Hand_L", "Upper_Arm_R", "Lower_Arm_R", "Hand_R", "Upper_Leg_L", "Lower_Leg_L", "Foot_L", "Upper_Leg_R", "Lower_Leg_R", "Foot_R" };
            foreach (var bone in AllBones.Values)
                Debug.Assert(bone.Name == jointOrder[j++]);

            Sphere = new Model(Primitives.Sphere, new SingleColorMaterial(new SingleColorShader3D()) { Color = new Vector4(1, 0, 0, 1) });
            Sphere.Transform.Scale = new Vector3(0.05f);

            var pairs = CreateVertexWeights(scene.Meshes[0].Bones, scene.Meshes[0].VertexCount);
            var ids = pairs.SelectMany(p => p.Item1).Cast<float>().ToVec4s();
            var weights = pairs.SelectMany(p => p.Item2).ToVec4s();

            {
                var geo = new Geometry();
                var mesh = scene.Meshes[0];
                geo.Positions = mesh.Vertices.Select(v => v.ToOpenTK()).ToList();
                geo.UVs = mesh.TextureCoordinateChannels[0].Select(uv => new Vector2(uv.X, uv.Y)).ToList();
                geo.Indicies = mesh.GetIndices().Cast<uint>().ToList();
                geo.Normals = mesh.Normals.Select(n => n.ToOpenTK()).ToList();
                geo.BoneIds = ids.ToList();
                geo.BoneWeights = weights.ToList();

                AlbedoTexture = TextureCache.Get("assets/textures/diffuse.png");
                material.diffuse = AlbedoTexture;
                SkinnedMesh = geo.ToVirtualVAO();
                OrignalModel = new Model(SkinnedMesh, material);
                SkeletonShader = new DynamicPixelShader(
                    "assets/shaders/AnimatedModel/VertexShader.vert",
                    "assets/shaders/AnimatedModel/FragmentShader.frag",
                    null,
                    "BoneWeights"
                );

                //var IdentityPoses = AllBones.Select(b => b.Value.BindPose.Matrix).ToArray();
                BonePosesUBO = new Buffer<Matrix4>(64, BufferTarget.UniformBuffer, BufferUsageHint.DynamicDraw, "Bone Poses");
                BonePosesUBO.Bind(2);
            }
        }


        // TODO: Ids are ints
        public (float[], float[])[] CreateVertexWeights(IEnumerable<Assimp.Bone> bones, int numVerts)
        {
            const float fillerValue = float.MaxValue;
            var vertcies = Enumerable.Repeat(fillerValue, numVerts)
                .Select(i => (
                    ids:     Enumerable.Repeat(i, 4).ToArray(),
                    weights: Enumerable.Repeat(i, 4).ToArray()
                )
            ).ToArray();
            int boneIdx = 0;
            foreach (var bone in bones)
            {
                foreach (var weight in bone.VertexWeights)
                {
                    var nextBlankSlot = vertcies[weight.VertexID].ids.IndexOf(fillerValue);
                    vertcies[weight.VertexID].ids[nextBlankSlot] = boneIdx;
                    nextBlankSlot = vertcies[weight.VertexID].weights.IndexOf(fillerValue);
                    vertcies[weight.VertexID].weights[nextBlankSlot] = weight.Weight;
                }
                boneIdx++;
            }

            foreach (var (ids, weights) in vertcies)
            {
                var sum = weights.Where(x => x != fillerValue).Sum();
                Debug.Assert(sum > 0.99999f && sum < 1.0001);
                for (int i = 0; i < 4; i++)
                    if (weights[i] != float.MaxValue)
                        Debug.Assert(ids[i] != float.MaxValue, "weight linking to no bone");
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

        public void GetAll(List<Assimp.Node> children, Assimp.Node self)
        {
            children.Add(self);
            foreach (var child in self.Children)
                GetAll(children, child);
        }

        public Bone CreateSkeleton(IDictionary<string, Bone> allBones, Assimp.Node node)
        {
            var bone = allBones[node.Name];
            foreach (var child in node.Children)
                CreateSkeleton(allBones, child);
            foreach (var child in node.Children)
                bone.Children.Add(allBones[child.Name]);

            return bone;
        }

        public override void Render()
        {
            updateCameraUBO(Camera.ProjectionMatrix, Camera.ViewMatrix);

            LineRenderer.AddLine(Vector3.Zero, Vector3.UnitX);
            LineRenderer.AddLine(Vector3.Zero, Vector3.UnitY);
            LineRenderer.AddLine(Vector3.Zero, Vector3.UnitZ);

            //DrawPlane(LineRenderer, 100, 100, 100, 100);

            //RenderOtherTest();
            RenderNormalTest();

            GL.Disable(EnableCap.DepthTest);
            LineRenderer.Render();
            GL.Enable(EnableCap.DepthTest);
        }

        private void RenderNormalTest()
        {
            //var rotation = (float)GameMillisecondsElapsed / 1000f;
            var rotation = 90;
            var modelMatrix = MathFunctions.CreateModelMatrix(
                new Vector3(0,0,0),
                new Quaternion(rotation * 0.0174533f, 0, 0), 
                new Vector3(1f)
            );
            float timeMs = (float)GameMillisecondsElapsed;
            var modelSpaceTransforms = skeleton.GetModelSpaceTransforms(skeleton.Animations[0], timeMs);
            var boneSpaceTransforms = skeleton.GetBoneSpaceTransforms(modelSpaceTransforms);
            RootNode.GetBoneSpaceTransforms(modelSpaceTransforms, boneSpaceTransforms);
            //RootNode.UpdateTransforms(timeMs, boneSpaceTransforms, modelSpaceTransforms, Matrix4.Identity);

            // Validation
            foreach (var mat in boneSpaceTransforms)
                Debug.Assert(mat != Matrix4.Identity, "Bone transform not set");

            BonePosesUBO.Update(boneSpaceTransforms);
            
            //OrignalModel.Render();

            SkeletonShader.Use();
            SkeletonShader.Set("ModelMatrix", modelMatrix);
            AlbedoTexture.Use();
            SkinnedMesh.Draw();

            DrawSkeleton(RootNode, modelSpaceTransforms, modelMatrix, modelMatrix);

            RenderImGui(modelSpaceTransforms);
        }

        void DrawSkeleton(Bone bone, Span<Matrix4> modelSpaceTransforms, Matrix4 parentTransform, Matrix4 modelMatrix)
        {
            var thisBoneMSTransform = modelSpaceTransforms[bone.Index] * modelMatrix;
            LineRenderer.AddLine(parentTransform.ExtractTranslation(), thisBoneMSTransform.ExtractTranslation());
            renderAxisHelper(LineRenderer, thisBoneMSTransform);

            //Sphere.Transform.Position = thisBone.ExtractTranslation();
            //Sphere.Render();

            foreach (var child in bone.Children)
                DrawSkeleton(child, modelSpaceTransforms, thisBoneMSTransform, modelMatrix);
        }

        void renderAxisHelper(DebugLineRenderer lineRenderer, Matrix4 modelMatrix)
        {
            var O = (new Vector4(0, 0, 0, 1) * modelMatrix).Xyz;
            var Y = (new Vector4(1, 0, 0, 1) * modelMatrix).Xyz;
            var X = (new Vector4(0, 1, 0, 1) * modelMatrix).Xyz;
            var Z = (new Vector4(0, 0, 1, 1) * modelMatrix).Xyz;

            lineRenderer.AddLine(O, X);
            lineRenderer.AddLine(O, Y);
            lineRenderer.AddLine(O, Z);
        }

        void RenderImGui(Matrix4[] modelSpaceTransforms)
        {
            ImGuiController?.Update(this, (float)(1000f/30f));

            ImGui.Begin("Window");
            DisplayBone(RootNode, modelSpaceTransforms);
            ImGui.End();

            ImGuiController.Render();
        }

        public void DrawPlane(DebugLineRenderer renderer, int width, int depth, int rows, int columns, Vector3 offset = default)
        {
            var topLeft = new Vector3(-width / 2, 0, -depth / 2);
            var topRight = new Vector3(width / 2, 0, -depth / 2);
            {
                var zStep = (float)depth / rows;
                var hLine = new Vector3(0, 0, zStep);
                for (float row = 0; row < rows; row++)
                    renderer.AddLine(offset + topLeft + hLine * row, offset + topRight + hLine * row);
            }
            var bottomLeft = new Vector3(-width / 2, 0, depth / 2);
            var bottomRight = new Vector3(width / 2, 0, depth / 2);
            {
                var xStep = (float)width / columns;
                var yLine = new Vector3(xStep, 0, 0);
                for (float column = 0; column < columns; column++)
                    renderer.AddLine(offset + bottomLeft + yLine * column, offset + topLeft + yLine * column);
            }

            renderer.AddLine(offset + topRight, offset + bottomRight);
            renderer.AddLine(offset + bottomLeft, offset + bottomRight);
        }

        private void DisplayBone(Bone parentNode, Span<Matrix4> modelSpaceTransforms)
        {
            if (ImGui.TreeNodeEx($"{parentNode.Name} {parentNode.OffsetFromParent.ExtractTranslation()} {modelSpaceTransforms[parentNode.Index].ExtractTranslation()}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                foreach (var child in parentNode.Children)
                {
                    DisplayBone(child, modelSpaceTransforms);
                }
                ImGui.TreePop();
            }
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
