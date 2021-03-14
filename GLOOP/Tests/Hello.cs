using GLOOP.Rendering;
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
            // Have to do this here until I do a Material class
            var projectionMatrix = Camera.ProjectionMatrix;
            var viewMatrix = Camera.ViewMatrix;
            duck.Render(projectionMatrix, viewMatrix);
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
