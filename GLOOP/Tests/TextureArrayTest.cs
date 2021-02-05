using GLOOP.Rendering;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace GLOOP.Tests
{
    public class TextureArrayTest : Window
    {
        private DebugCamera Camera;
        private Entity Model;
        private Entity Model2;
        private Entity Model3;

        public TextureArrayTest(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            Camera = new DebugCamera(new Vector3(0, 1, 3), new Vector3(), 90);
        }

        protected override void OnLoad()
        {
            var random = new Random();
            ushort dimention = 4;
            var colours = new Vector4[dimention * dimention];
            void randomizeColours()
            {
                for (var i=0; i<colours.Length; i++)
                {
                    colours[i].X = (float)random.NextDouble();
                    colours[i].Y = (float)random.NextDouble();
                    colours[i].Z = (float)random.NextDouble();
                    colours[i].W = 1;
                }
            }
            var textureArray = new TextureArray(
                new TextureShape(
                    dimention, 
                    dimention,
                    false,
                    PixelInternalFormat.Rgb,
                    TextureWrapMode.Repeat,
                    TextureMinFilter.Nearest
                ),
                3
            );
            randomizeColours();
            textureArray.WriteSubData(0, colours);
            randomizeColours();
            textureArray.WriteSubData(1, colours);
            randomizeColours();
            textureArray.WriteSubData(2, colours);

            var assimp = new Assimp.AssimpContext();
            var shader = new TextureArrayShader();
            var material = new TextureArrayMaterial(shader);
            material.TextureArray = textureArray;
            Model = new Entity("assets/models/plane.dae", assimp, material.Clone());
            Model.Transform.Scale *= 10000f;
            Model.Transform.Position.X = -1.5f;
            ((TextureArrayMaterial)Model.Renderables[0].material).Slice = 0;

            Model2 = new Entity("assets/models/plane.dae", assimp, material.Clone());
            Model2.Transform.Scale *= 10000f;
            ((TextureArrayMaterial)Model2.Renderables[0].material).Slice = 1;

            Model3 = new Entity("assets/models/plane.dae", assimp, material.Clone());
            Model3.Transform.Position.X = 1.5f;
            Model3.Transform.Scale *= 10000f;
            ((TextureArrayMaterial)Model3.Renderables[0].material).Slice = 2;


            base.OnLoad();
        }

        public override void Render()
        {
            // Have to do this here until I do a Material class
            var projectionMatrix = MathFunctions.CreateProjectionMatrix(Size.X, Size.Y, Camera.FOV, 0.1f, 10000);
            var viewMatrix = MathFunctions.CreateViewMatrix(Camera.Position, Camera.Rotation);
            Model.Render(projectionMatrix, viewMatrix);
            Model2.Render(projectionMatrix, viewMatrix);
            Model3.Render(projectionMatrix, viewMatrix);
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
