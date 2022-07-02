using GLOOP;
using GLOOP.Rendering;
using GLOOP.Rendering.Materials;
using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace AnimationTest
{
    internal class AnimationTest : Window
    {
        private readonly ImGuiController ImGuiController;
        private Camera Camera;
        private ModelLoader Model;

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
            Model = new ModelLoader("assets/models/model.dae", assimp, material);
            Model.Models[0].Transform.Scale *= 0.1f;
            Model.Models[0].Transform.Rotation = new Quaternion(90, 0, 0);
            Model.Models[0].Material.SetTextures(TextureCache.Get("assets/textures/diffuse.png"), null, null, null);
        }

        public override void Render()
        {
            updateCameraUBO(Camera.ProjectionMatrix, Camera.ViewMatrix);

            Model.Render();

            RenderImGui();
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
