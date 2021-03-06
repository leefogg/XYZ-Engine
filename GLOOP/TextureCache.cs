using GLOOP.Rendering;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace GLOOP
{
    public static class TextureCache
    {
        private static Dictionary<string, TextureSlice> TexturesSlices = new Dictionary<string, TextureSlice>();
        private static Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();

        public static TextureSlice GetSlice(
            string path,
            PixelInternalFormat format = PixelInternalFormat.Rgba,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            TextureMinFilter filterMode = TextureMinFilter.Linear,
            bool hasMipMaps = false
        ) {
            if (TexturesSlices.TryGetValue(path, out TextureSlice tex))
                return tex;

            return TexturesSlices[path] = TextureArrayManager.Get(
                path,
                format, 
                wrapMode,
                filterMode,
                hasMipMaps
            );
        }

        public static Texture2D Get(
            string path,
            PixelInternalFormat format = PixelInternalFormat.Rgba,
            TextureMinFilter minFilter = TextureMinFilter.Linear,
            TextureMinFilter magFilter = TextureMinFilter.Linear,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            bool hasMipMaps = false
        ) {
            if (Textures.TryGetValue(path, out Texture2D tex))
                return tex;

            try { 
                var shape = new TextureParams()
                {
                    MinFilter = minFilter,
                    MagFilter = magFilter,
                    WrapMode = wrapMode,
                    GenerateMips = hasMipMaps,
                    InternalFormat = format
                };
                return Textures[path] = new Texture2D(
                    path,
                    shape
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(path);
                return null;
            }
        }

        public static bool TryGet(string name, out TextureSlice tex) => TexturesSlices.TryGetValue(name, out tex);
        public static bool TryGet(string name, out Texture2D tex) => Textures.TryGetValue(name, out tex);
    }
}
