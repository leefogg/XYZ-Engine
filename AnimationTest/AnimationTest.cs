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
        private VirtualVAO VAO;
        private Model Model;
        private DynamicPixelShader BoneWeightsShader;
        private Buffer<Matrix4> BonePosesUBO;
        private Dictionary<string, Bone> AllBones;
        private Bone RootNode;
        private Model Sphere;
        private Texture2D AlbedoTexture;
        private Matrix4[] boneTransforms;

        public AnimationTest(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            Camera = new Camera(new Vector3(-0.34869373f, 0.5f, 21.04657f), new Vector3(), 90)
            {
                CameraController = new PCCameraController()
            };

            ImGuiController = new ImGuiController(ClientSize.X, ClientSize.Y);
            LineRenderer = new DebugLineRenderer(1024);
        }

        protected override void OnLoad()
        {
            base.OnLoad();

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

            var anim = scene.Animations[0];
            var skeleton = scene.RootNode.Children.First(child => child.Name == "Armature");
            var bones = new List<Assimp.Node>();
            GetAll(bones, skeleton);
            bones = bones.Skip(1).ToList();

            AllBones = new Dictionary<string, Bone>();
            for (int i = 0; i < scene.Meshes[0].BoneCount; i++)
            {
                var bone = scene.Meshes[0].Bones[i];
                var newBone = new Bone(bone.Name);
                newBone.ID = i;

                var transform = bones[i].Transform;
                //transform.Inverse();
                transform.Decompose(out var scale, out var rot, out var pos);
                newBone.OffsetToParent = new StaticTransform(pos.ToOpenTK(), scale.ToOpenTK(), rot.ToOpenTK());
                transform = bone.OffsetMatrix;
                //transform.Inverse();
                transform.Decompose(out scale, out rot, out pos);
                newBone.InvBindPose = new StaticTransform(pos.ToOpenTK(), scale.ToOpenTK(), rot.ToOpenTK());

                newBone.AddAnimation(anim.NodeAnimationChannels.First(c => c.NodeName == bone.Name), (float)anim.TicksPerSecond);
                AllBones.Add(newBone.Name, newBone);
            }
            RootNode = CreateSkeleton(AllBones, skeleton.Children[0]);

            for (int i = 0; i < bones.Count; i++)
                Debug.Assert(AllBones[bones[i].Name].ID == i, "Mismatched IDs");

            Sphere = new Model(Primitives.Sphere, new SingleColorMaterial(new SingleColorShader3D()) { Color = new Vector4(1,0,0,1)});
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
                VAO = geo.ToVirtualVAO();
                Model = new Model(VAO, material);
                BoneWeightsShader = new DynamicPixelShader(
                    "assets/shaders/SkinWeights/VertexShader.vert",
                    "assets/shaders/SkinWeights/FragmentShader.frag",
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

            var modelMatrix = new StaticTransform(new Vector3(0), new Vector3(1f), new Quaternion(0f * 0.0174533f, 0, 0)).Matrix;
            boneTransforms = new Matrix4[AllBones.Count];
            RootNode.UpdateTransforms((float)GameMillisecondsElapsed, boneTransforms, modelMatrix);

            // Validation
            foreach (var mat in boneTransforms)
            {
                //Debug.Assert(mat != Matrix4.Identity, "Bone transform not set");
            }

            BonePosesUBO.Update(boneTransforms);

            //Model.Render();
            
            BoneWeightsShader.Use();
            BoneWeightsShader.Set("ModelMatrix", modelMatrix);
            AlbedoTexture.Use();
            VAO.Draw();

            GL.Disable(EnableCap.DepthTest);
            foreach (var bone in AllBones.Values)
            {
                Sphere.Transform.Position = boneTransforms[bone.ID].ExtractTranslation();
                //Sphere.Transform.Scale = new Vector3(bone.BindPose.Scale);
                Sphere.Transform.Scale = new Vector3(0.1f);
                Sphere.Render();
            }
            GL.Enable(EnableCap.DepthTest);

            RenderImGui();

            LineRenderer.Render();
        }


        void RenderImGui()
        {
            ImGuiController?.Update(this, (float)(1000f/30f));

            ImGui.Begin("Window");
            ImGui.BeginChild("Scene");
            DisplayBone(RootNode);
            ImGui.EndChild();
            ImGui.End();

            ImGuiController.Render();
        }

        Random r = new Random();
        private void DisplayBone(Bone parentNode)
        {
            if (ImGui.TreeNodeEx($"{parentNode.Name} {parentNode.OffsetToParent.Matrix.ExtractTranslation()} {parentNode.CurrentTransform.ExtractTranslation()}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                foreach (var child in parentNode.Children)
                {
                    DisplayBone(child);
                    LineRenderer.AddLine(parentNode.CurrentTransform.ExtractTranslation(), child.CurrentTransform.ExtractTranslation());
                }
                ImGui.TreePop();
            }
        }

        private void DisplayFolder(string folder)
        {
            if (ImGui.TreeNodeEx(folder[folder.LastIndexOf("\\")..]))
                foreach (var subFolder in Directory.EnumerateDirectories(folder))
                    DisplayFolder(subFolder);
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
