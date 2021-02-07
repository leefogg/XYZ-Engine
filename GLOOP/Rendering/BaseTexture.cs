using OpenTK.Graphics.OpenGL4;
using System;
using System.Linq;
using static OpenTK.Graphics.OpenGL4.GL;
using static OpenTK.Graphics.OpenGL4.GL.Arb;

namespace GLOOP.Rendering
{
    public abstract class BaseTexture : IDisposable
    {
        private static readonly int[] BoundTextures = new int[16];
        private static readonly int[] TexturesToBind = new int[BoundTextures.Length];

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
            DeleteTexture(Handle);
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

        public static void Use(Texture[] textures, TextureUnit firstUnit)
        {
            var first = firstUnit - TextureUnit.Texture0;
            var i = first;
            foreach (var tex in textures)
                TexturesToBind[i++] = tex.Handle;

            BindTextures(first, textures.Length, TexturesToBind);

            for (i = 0; i < TexturesToBind.Length; i++)
                BoundTextures[i + first] = TexturesToBind[i];
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
