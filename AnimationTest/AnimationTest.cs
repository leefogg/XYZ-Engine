﻿using GLOOP;
using GLOOP.Rendering;
using GLOOP.Rendering.Materials;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.ComponentModel;
namespace AnimationTest
{
    internal class AnimationTest : Window
    {
        private Camera Camera;
        private Entity duck;

        public AnimationTest(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            Camera = new Camera(new Vector3(0.0f, 0.05f, 0.14f), new Vector3(), 70)
            {
                CameraController = new PCCameraController()
            };
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            var assimp = new Assimp.AssimpContext();
            var shader = new FullbrightShader();
            var material = new FullbrightMaterial(shader);
            duck = new Entity("assets/models/model.dae", assimp, material);
            duck.Models[0].Transform.Scale *= 0.01f;
            duck.Models[0].Material.SetTextures(TextureCache.Get("assets/textures/diffuse.png"), null, null, null);
        }

        public override void Render()
        {
            updateCameraUBO(Camera.ProjectionMatrix, Camera.ViewMatrix);

            duck.Render();
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
