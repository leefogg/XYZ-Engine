using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public abstract class BaseTexture : IDisposable
    {
        private static readonly int[] BoundTextures = new int[16];

        public readonly int Handle = GL.GenTexture();
        public readonly TextureTarget Type;

        public BaseTexture(TextureTarget type)
        {
            Type = type;
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
                GL.ActiveTexture(unit);
                GL.BindTexture(type, handle);

                BoundTextures[unit - TextureUnit.Texture0] = handle;
            }
        }
    }
}
