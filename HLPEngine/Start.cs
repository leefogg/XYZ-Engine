using GLOOP.Tests;
using OpenTK;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.HPL
{
    public class Start
    {
        public static void Main(string[] _)
        {
            var gameWindowSettings = new GameWindowSettings();
            var nativeWindowSettings = new NativeWindowSettings {
                API = ContextAPI.OpenGL,
                APIVersion = new Version(4, 3),
                Profile = ContextProfile.Compatability,
                Flags = ContextFlags.Debug,
                Size = new Vector2i(1920, 1080),
                IsEventDriven = false,
                Title = "prototype engine"
            };
            nativeWindowSettings.Location = new Vector2i(
                3840 / 2 - nativeWindowSettings.Size.X / 2,
                2160 / 2 - nativeWindowSettings.Size.Y / 2
            );
            using var window = new Game(gameWindowSettings, nativeWindowSettings);
            window.VSync = VSyncMode.On;
            window.Run();
        }
    }
}
