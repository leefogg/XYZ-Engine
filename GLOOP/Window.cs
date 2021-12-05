using GLOOP.Rendering;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace GLOOP
{
    public abstract class Window : GameWindow
    {
        public static ulong FrameNumber { get; protected set; }
        public static int FPS;

        private static readonly DateTime startTime = DateTime.Now;
        public static float MillisecondsElapsed => (DateTime.Now - startTime).Milliseconds;

        private int framesThisSecond;
        private DateTime lastSecond;
        public bool bindMouse = true;

#if DEBUG
        private DebugProc _debugProcCallback = DebugCallback;
        private GCHandle _debugProcCallbackHandle;
#endif

        public static event EventHandler<Vector2i> OnResized;

        private Buffer<Matrix4> cameraBuffer;

        protected Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnResize(ResizeEventArgs e)
        {

            OnResized?.Invoke(this, Size);

            base.OnResize(e);
        }

        protected override void OnLoad()
        {
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(0, 0, 0, 1);

            Mouse.Init(this);

#if DEBUG
            _debugProcCallbackHandle = GCHandle.Alloc(_debugProcCallback);
            GL.DebugMessageCallback(_debugProcCallback, IntPtr.Zero);
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
#endif

            setupCameraUniformBuffer();

            lastSecond = DateTime.Now;

            base.OnLoad();
        }

        private static void DebugCallback(DebugSource source,
                                  DebugType type,
                                  int id,
                                  DebugSeverity severity,
                                  int messageLength,
                                  IntPtr message,
                                  IntPtr userParam)
        {
            if (type == DebugType.DebugTypeOther)
                return;

            string messageString = Marshal.PtrToStringAnsi(message, messageLength);

            var excludedTypes = new[]
            {
                DebugType.DebugTypePushGroup,
                DebugType.DebugTypePopGroup
            };
            if (excludedTypes.Contains(type))
                return;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"{severity} {type} | {messageString}");
            Console.ForegroundColor = ConsoleColor.White;

            if (type == DebugType.DebugTypeError)
                throw new Exception(messageString);
        }

        private void setupCameraUniformBuffer()
        {
            cameraBuffer = new Buffer<Matrix4>(5, BufferTarget.UniformBuffer, BufferUsageHint.StreamRead, "CameraData");
            cameraBuffer.BindRange(0, 0);
        }

        protected void updateCameraUBO(in Matrix4 projectionMatrix, in Matrix4 viewMatrix)
        {
            var projectionView = new Matrix4();
            MatrixExtensions.Multiply(projectionMatrix, viewMatrix, ref projectionView);
            var inverseView = viewMatrix.Inverted();
            var inverseProjection = projectionView.Inverted();
            cameraBuffer.Update(new[] { 
                projectionMatrix, 
                viewMatrix, 
                projectionView,
                inverseView,
                inverseProjection
            });
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Render();

            SwapBuffers();

            NewFrame();

            base.OnRenderFrame(args);
        }

        public void NewFrame()
        {
            FrameNumber++;
            var now = DateTime.Now;
            if (now > lastSecond + TimeSpan.FromSeconds(1))
            {
                FPS = framesThisSecond;
                lastSecond = now;
                framesThisSecond = 0;
            }
            else
            {
                framesThisSecond++;
            }
        }

        public virtual void Render()
        {

        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            Mouse.Update();

            if (IsFocused)
            {
                var input = KeyboardState;
                if (input.IsKeyDown(Keys.Escape))
                    Close();

                if (Mouse.CurrentState.IsButtonDown(MouseButton.Right))
                    Mouse.Grabbed = false;
                if (bindMouse && Mouse.CurrentState.IsButtonDown(MouseButton.Left))
                    Mouse.Grabbed = true;
            }

            base.OnUpdateFrame(args);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }
    }
}
