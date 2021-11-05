using GLOOP.Rendering;
using GLOOP.Rendering.Materials;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System.ComponentModel;

namespace GLOOP.Tests
{
    public class Hello : Window
    {
        private DebugCamera Camera;
        private Entity duck;

        public Hello(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) 
            : base(gameWindowSettings, nativeWindowSettings)
        {
            Camera = new DebugCamera(new Vector3(0,1,3), new Vector3(), 90);
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            var assimp = new Assimp.AssimpContext();
            var shader = new FullbrightShader();
            var material = new FullbrightMaterial(shader);
            duck = new Entity("assets/models/duck.dae", assimp, material);
            duck.Models[0].Transform.Scale *= 0.01f;
            duck.Models[0].Material.SetTextures(TextureCache.Get("assets/textures/duck.bmp"), null, null, null);
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
    }
}
