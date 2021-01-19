using GLOOP.Rendering;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public static class TextureCache
    {
        private static Dictionary<string, TextureSlice> TexturesSlices = new Dictionary<string, TextureSlice>();
        private static Dictionary<string, Texture> Textures = new Dictionary<string, Texture>();

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

        public static Texture Get(
            string path,
            PixelInternalFormat format = PixelInternalFormat.Rgba,
            TextureMinFilter minFilter = TextureMinFilter.Linear,
            TextureMinFilter magFilter = TextureMinFilter.Linear,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            bool hasMipMaps = false
        ) {
            if (Textures.TryGetValue(path, out Texture tex))
                return tex;

            return Textures[path] = new Texture(
                path,
                new TextureParams()
                {
                    MinFilter = minFilter,
                    MagFilter = magFilter,
                    WrapMode = wrapMode,
                    GenerateMips = hasMipMaps,
                    InternalFormat = format
                }
            );
        }

        public static bool TryGet(string name, out TextureSlice tex) => TexturesSlices.TryGetValue(name, out tex);
        public static bool TryGet(string name, out Texture tex) => Textures.TryGetValue(name, out tex);
    }
}
