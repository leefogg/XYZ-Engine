using GLOOP.Extensions;
using GLOOP.Rendering;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using static OpenTK.Graphics.OpenGL4.GL;

namespace GLOOP.Tests
{
    public class Hello : Window
    {
        private DebugCamera Camera;
        private Model Model1;
        private Model Model2;
        private Model Plane;
        private SingleColorMaterial singleColorMaterial;
        private Vector3 sincos = new Vector3();

        public Hello(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) 
            : base(gameWindowSettings, nativeWindowSettings)
        {
            Camera = new DebugCamera(new Vector3(0,1,3), new Vector3(), 90);
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            new Texture("assets/textures/black.png", PixelInternalFormat.Rgb);

            var assimp = new Assimp.AssimpContext();
            var shader = new FullbrightShader();
            var material = new FullbrightMaterial(shader);
            Model1 = new Model("assets/models/pyramid.dae", assimp, material);
            Model1.Scale *= 10000f;
            //Model1.Scale = new Vector3(0.2f, 0.4f, 0.9f);
            Model1.Position.X = -1.5f;
            Model1.Position.Y = 0.5f;
            Model1.Renderables[0].material.SetTextures(TextureCache.Get("assets/textures/duck.bmp"), null, null, null);

            Model2 = new Model("assets/models/sphere.obj", assimp, material);
            Model2.Position.X = 1.5f;
            Model2.Scale *= 0.1f;
            Model2.Renderables[0].material.SetTextures(TextureCache.Get("assets/textures/duck.bmp"), null, null, null);

            Plane = new Model("assets/models/plane.dae", assimp, material);
            Plane.Scale *= new Vector3(100000f, 1f, 100000f);
            //Plane.Scale = new Vector3(1 / Plane.WorldScale.X, 1 / Plane.WorldScale.Y, 1 / Plane.WorldScale.Z);
            Plane.Renderables[0].material.SetTextures(TextureCache.Get("assets/textures/gray.png"), null, null, null);

            var singleColorShader = new SingleColorShader();
            singleColorMaterial = new SingleColorMaterial(singleColorShader);
            singleColorMaterial.Color = new Vector4(1);
        }

        public override void Render()
        {
            // Have to do this here until I do a Material class
            var projectionMatrix = Camera.ProjectionMatrix;
            var viewMatrix = Camera.ViewMatrix;
            Model1.Render(projectionMatrix, viewMatrix);
            //Model2.Render(projectionMatrix, viewMatrix);
            Plane.Render(projectionMatrix, viewMatrix);

            var rotation = Quaternion.FromAxisAngle(Vector3.UnitX, sincos.X) * Quaternion.FromAxisAngle(Vector3.UnitY, sincos.Y) * Quaternion.FromAxisAngle(Vector3.UnitZ, sincos.Z);
            sincos += new Vector3(0.01f, 0.00f, 0.01f);
            Model1.Rot = rotation;
            Model1.RenderBoundingBox(projectionMatrix, viewMatrix);

            var verts = new List<Vector3>(8);
            Model1.OriginalBoundingBox
                .Translated(-Model1.OriginalBoundingBox.Center)
                .Scaled(Model1.Scale, Vector3.Zero)
                .GetCorners(verts);
            verts.RotateAround(Model1.Rot.Inverted(), Model1.OriginalBoundingBox.Center);
            foreach (var vert in verts)
            {
                Model2.Position = vert + new Vector3(0,1,0);
                Model2.Render(projectionMatrix, viewMatrix);
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
    }
}
