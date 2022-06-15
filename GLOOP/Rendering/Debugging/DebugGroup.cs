using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Debugging
{
    public ref struct DebugGroup
    {
        private static int stackDepth = 0;

        public DebugGroup(string name)
        {
#if DEBUG
            name = name[..Math.Min(name.Length, Globals.MaxDebugGroupNameLength)];
            GL.PushDebugGroup(DebugSourceExternal.DebugSourceApplication, stackDepth++, name.Length, name);

            System.Diagnostics.Debug.Assert(stackDepth < 30, "DebugGroups are unbalenced. Check all groups are being disposed.");
#endif
        }

        public void Dispose()
        {
#if DEBUG
            GL.PopDebugGroup();
            stackDepth--;
#endif
        }
    }
}
