﻿using GLOOP.Tests;
using HLPEngine;
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
            uint width = 1920;
            uint height = 1080;
#if VR
            VRSystem.SetUpOpenVR();
            VRSystem.GetFramebufferSize(out width, out height);
#endif

            var gameWindowSettings = new GameWindowSettings();
            var nativeWindowSettings = new NativeWindowSettings {
                API = ContextAPI.OpenGL,
                APIVersion = new Version(4, 3),
                Profile = ContextProfile.Core,
#if Debug
                Flags = ContextFlags.Debug,
#else
                Flags = ContextFlags.Default,
#endif
                Size = new Vector2i((int)width, (int)height),
                IsEventDriven = false,
                Title = "prototype engine"
            };
            nativeWindowSettings.Location = new Vector2i(
                3840 / 2 - nativeWindowSettings.Size.X / 2,
                2160 / 2 - nativeWindowSettings.Size.Y / 2
            );
            using var window = new Game(gameWindowSettings, nativeWindowSettings);
            window.VSync =
#if VR
            VSyncMode.Off;
#else
            VSyncMode.On;
#endif
            window.Run();
        }
    }
}