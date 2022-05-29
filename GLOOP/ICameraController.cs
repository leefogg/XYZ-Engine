using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public interface ICameraController
    {
        public void Update(Camera cam, OpenTK.Windowing.GraphicsLibraryFramework.KeyboardState keyboardState);
    }
}
