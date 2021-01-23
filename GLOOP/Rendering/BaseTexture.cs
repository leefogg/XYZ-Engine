using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;
using static OpenTK.Graphics.OpenGL4.GL;
using static OpenTK.Graphics.OpenGL4.GL.Arb;

namespace GLOOP.Rendering
{
    public abstract class BaseTexture : IDisposable
    {
        private static readonly int[] BoundTextures = new int[16];

        private Lazy<ulong> bindlessHandle;
        public readonly int Handle = GenTexture();
        public ulong BindlessHandle => bindlessHandle.Value;
        public readonly TextureTarget Type;

        public BaseTexture(TextureTarget type)
        {
            Type = type;

            bindlessHandle = new Lazy<ulong>(makeTexureResident);
        }

        public void Dispose()
        {
            GL.DeleteTexture(Handle);
        }

        public void Use(TextureUnit unit = TextureUnit.Texture0)
        {
            Use(Type, Handle, unit);
        }

        public static void Use(TextureTarget type, int handle, TextureUnit unit = TextureUnit.Texture0)
        {
            if (BoundTextures[unit - TextureUnit.Texture0] != handle)
            {
                ActiveTexture(unit);
                BindTexture(type, handle);

                BoundTextures[unit - TextureUnit.Texture0] = handle;
            }
        }

        private ulong makeTexureResident()
        {
            Use();

            var handle = (ulong)GetTextureHandle(Handle);
            MakeTextureHandleResident(handle);
            return handle;
        }
    }
}
