using GLOOP;
using GLOOP.Animation;
using GLOOP.Extensions;
using GLOOP.Rendering;
using GLOOP.Rendering.Materials;
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
        private Camera Camera;
        private Model Model;
        private TransformTimeline Timeline;
        private Dictionary<string, Bone> AllBones;
        private Bone RootNode;
        private Model Sphere;

        public AnimationTest(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            Camera = new Camera(new Vector3(0, 0.5f, 1.5f), new Vector3(), 90)
            {
                CameraController = new PCCameraController()
            };

            ImGuiController = new ImGuiController(ClientSize.X, ClientSize.Y);
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            var assimp = new Assimp.AssimpContext();
            var shader = new FullbrightShader();
            var material = new FullbrightMaterial(shader);
            var path = "assets/models/model.dae";
            Model = new ModelLoader(path, assimp, material).Models[0];
            Model.Transform.Scale *= 0.1f;
            //Model.Transform.Rotation = new Quaternion(90, 0, 0);
            Model.Material.SetTextures(TextureCache.Get("assets/textures/diffuse.png"), null, null, null);

            var steps = Assimp.PostProcessSteps.LimitBoneWeights;
            var scene = assimp.ImportFile(path, steps);

            var anim = scene.Animations[0];
            var skeleton = scene.RootNode.Children.First(child => child.Name == "Armature");
            var bones = new List<Assimp.Node>();
            GetAll(bones, skeleton);

            AllBones = new Dictionary<string, Bone>();
            for (int i = 0; i < scene.Meshes[0].BoneCount; i++)
            {
                var bone = scene.Meshes[0].Bones[i];
                var newBone = new Bone(bone.Name);
                newBone.InitialTransform = bones.First(b => b.Name == bone.Name).Transform.ToOpenTK();
                newBone.InitialTransform = bone.OffsetMatrix.ToOpenTK();
                newBone.AssignVertexWeights(bone.VertexWeights);
                newBone.AddAnimation(anim.NodeAnimationChannels.First(c => c.NodeName == bone.Name), (float)anim.TicksPerSecond);
                AllBones.Add(newBone.Name, newBone);
            }
            RootNode = CreateSkeleton(AllBones, skeleton.Children[0]);

            Sphere = new Model(Primitives.Sphere, new SingleColorMaterial(new SingleColorShader3D()) { Color = new Vector4(1,0,0,1)});
            Sphere.Transform.Scale = new Vector3(0.01f);

            var vertexWeights = CreateVertexWeights(scene.Meshes[0].Bones);
        }

        public List<float>[] CreateVertexWeights(IEnumerable<Assimp.Bone> bones)
        {
            var weights = bones.SelectMany(bone => bone.VertexWeights).ToList();
            var numVerts = weights.Select(w => w.VertexID).Distinct().Count();
            var vertcies = Enumerable.Range(0, numVerts).Select(i => new List<float>()).ToArray();
            foreach (var weight in weights)
                vertcies[weight.VertexID].Add(weight.Weight);

            foreach (var vertex in vertcies)
            {
                Debug.Assert(vertex.Count <= 4);
                var sum = vertex.Sum();
                Debug.Assert(sum > 0.99999f && sum < 1.0001);
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

            Model.Render();

            GL.Disable(EnableCap.DepthTest);
            foreach (var bone in AllBones.Values)
            {
                Sphere.Transform.Position = bone.InitialTransform.ExtractTranslation();
                Sphere.Transform.Position *= 0.1f;
                Sphere.Render();
            }
            GL.Enable(EnableCap.DepthTest);

            //RenderImGui();
        }


        void RenderImGui()
        {
            ImGuiController?.Update(this, (float)(1000f/30f));

            ImGui.Begin("Window");
            ImGui.BeginChild("Scene");
            DisplayFolder(@"C:\Users\Lee\Documents\GitHub\XYZ-Engine\");
            ImGui.EndChild();
            ImGui.End();

            ImGuiController.Render();
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
