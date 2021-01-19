using OpenTK;
using OpenTK.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public static class Mouse
    {
        private static GameWindow Window;

        public static MouseState LastState { get; private set; }
        public static MouseState CurrentState { get; private set; }

        public static bool Grabbed
        {
            get => Window.CursorGrabbed;
            set {
                if (!value)
                    Window.CursorVisible = true;
                Window.CursorGrabbed = value;
                //Window.CursorVisible = !value;
            }
        }

        public static void Init(GameWindow window)
        {
            Window = window;
        }

        public static void Update()
        {
            LastState = CurrentState;
            CurrentState = Window.MouseState;
            //if (Grabbed)
            //    OpenTK.Input.Mouse.SetPosition(Window.X + Window.Width / 2, Window.Y + Window.Height / 2);
        }
    }
}
