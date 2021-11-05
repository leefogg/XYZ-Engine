﻿using GLOOP.Rendering;
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
        private Entity Plane1;
        private Entity Plane2;
        private Entity Plane3;

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
            Plane1 = new Entity("assets/models/plane.dae", assimp, new TextureArrayMaterial(shader) { 
                TextureArray = textureArray, 
                Slice = 0 
            });
            Plane1.Models[0].Transform.Scale *= 10000f;
            Plane1.Models[0].Transform.Position.X = -1.5f;

            Plane2 = new Entity("assets/models/plane.dae", assimp, new TextureArrayMaterial(shader)
            {
                TextureArray = textureArray,
                Slice = 1
            });
            Plane2.Models[0].Transform.Scale *= 10000f;

            Plane3 = new Entity("assets/models/plane.dae", assimp, new TextureArrayMaterial(shader)
            {
                TextureArray = textureArray,
                Slice = 2
            });
            Plane3.Models[0].Transform.Position.X = 1.5f;
            Plane3.Models[0].Transform.Scale *= 10000f;

            base.OnLoad();
        }

        public override void Render()
        {
            updateCameraUBO(Camera.ProjectionMatrix, Camera.ViewMatrix);

            Plane1.Render();
            Plane2.Render();
            Plane3.Render();
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
