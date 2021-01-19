using GLOOP.Tests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GLOOP.Rendering
{
    public class DynamicPixelShader : StaticPixelShader
    {
        private int tempHandle;
        private bool changed = false;
        private ulong changedFrame;
        private string vertPath;
        private string fragPath;
        private IDictionary<string, string> defines;
        private FileSystemWatcher watcher;

        public DynamicPixelShader(string vertPath, string fragPath, IDictionary<string, string> defines = null, string name = null) : base(vertPath, fragPath, defines, name)
        {
            this.vertPath = vertPath;
            this.fragPath = fragPath;
            this.defines = defines;

            watcher = new FileSystemWatcher(Path.GetDirectoryName(vertPath))
            {
                NotifyFilter = NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.FileName
                                 | NotifyFilters.DirectoryName
            };

            watcher.Changed += recompile;

            watcher.EnableRaisingEvents = true;
        }

        private void recompile(object sender, FileSystemEventArgs e)
        {
            changedFrame = Window.FrameNumber;
            changed = true;
        }

        public override void Use()
        {
            if (changed && Window.FrameNumber == changedFrame + 2) {
                try {
                    tempHandle = load(vertPath, fragPath, defines);
                } catch (Exception) {
                    changed = false;
                }
            }
            else if (changed && Window.FrameNumber == changedFrame + 5) {
                Dispose();
                Handle = tempHandle;
                LoadUniforms();
                Reload();
                changed = false;
            }

            base.Use();
        }

        protected virtual void Reload() { }
    }
}
