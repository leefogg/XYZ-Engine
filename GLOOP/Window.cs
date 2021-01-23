using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace GLOOP
{
    public abstract class Window : GameWindow
    {
        public static ulong FrameNumber { get; protected set; }
        private int framesThisSecond;
        private DateTime lastSecond;
        public static int FPS;

#if DEBUG
        private DebugProc _debugProcCallback = DebugCallback;
        private GCHandle _debugProcCallbackHandle;
#endif
        public static int Width { get; private set; }
        public static int Height { get; private set; }

        public static event EventHandler<Vector2i> OnResized;

        protected Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
            Width = Size.X;
            Height = Size.Y;
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            Width = Size.X;
            Height = Size.Y;

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

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"{severity} {type} | {messageString}");
            Console.ForegroundColor = ConsoleColor.White;

            if (type == DebugType.DebugTypeError)
                throw new Exception(messageString);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Render();

            SwapBuffers();

            FrameNumber++;
            var now = DateTime.Now;
            if (now > lastSecond + TimeSpan.FromSeconds(1))
            {
                FPS = framesThisSecond;
                lastSecond = now;
            }
            else
            {
                framesThisSecond++;
            }

            base.OnRenderFrame(args);
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
                if (Mouse.CurrentState.IsButtonDown(MouseButton.Left))
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
