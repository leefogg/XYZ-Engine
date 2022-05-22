using GLOOP.Util.Structures;
using OpenTK.Graphics.OpenGL4;
using static OpenTK.Graphics.OpenGL4.GL;
using static OpenTK.Graphics.OpenGL4.GL.Arb;

namespace GLOOP.Rendering
{
    public abstract class Texture : System.IDisposable
    {
        private static readonly int[] BoundTextures = new int[Globals.MaxTextureUnits];
        private static readonly int[] TexturesToBind = new int[BoundTextures.Length];

        public static readonly Texture2D Error = TextureCache.Get("assets/textures/error.png");
        public static readonly Texture2D Gray = TextureCache.Get("assets/textures/gray.png");
        public static readonly Texture2D Black = TextureCache.Get("assets/textures/black.png");

        private ulong? bindlessHandle;
        public readonly int Handle = GenTexture();
        public ulong BindlessHandle => bindlessHandle ??= makeTexureResident();
        public readonly TextureTarget Type;

        public Texture(TextureTarget type)
        {
            Type = type;
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
                Metrics.TextureSetBinds++;
            }
        }

        public static void Use(Texture2D[] textures, TextureUnit firstUnit)
        {
            var first = firstUnit - TextureUnit.Texture0;
            var i = first;
            var anyChanges = false;
            foreach (var tex in textures)
            {
                var handle = tex.Handle;
                TexturesToBind[i] = handle;
                anyChanges |= BoundTextures[i + first] != handle;
                i++;
            }

            if (!anyChanges)
                return;

            BindTextures(first, textures.Length, TexturesToBind);

            for (i = 0; i < textures.Length; i++)
                BoundTextures[i + first] = TexturesToBind[i];

            Metrics.TextureSetBinds++;
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
