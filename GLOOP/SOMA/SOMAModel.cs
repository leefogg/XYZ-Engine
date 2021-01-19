using GLOOP.Rendering;
using GLOOP.SOMA;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using static OpenTK.Graphics.OpenGL.GL;

namespace GLOOP.SOMA
{
    public class SOMAModel : Model {
        public static Vector3 Offset = new Vector3(0,0,0);

        public string ResourcePath { get; }

        public SOMAModel(string path,  Assimp.AssimpContext assimp, DeferredRenderingGeoMaterial material) : base(path, assimp, material) {
            ResourcePath = path;
        }

        private static T Deserialize<T>(string path)
        {
            var primitivesSerializer = new XmlSerializer(typeof(T));
            using var file = File.OpenRead(path);
            return (T)primitivesSerializer.Deserialize(file);
        }

        public override void GetTextures(
            string diffusePath,
            string normalPath,
            string specularPath,
            string illumPath,
            string currentFolder,
            out Texture diffuseTex,
            out Texture normalTex,
            out Texture specularTex,
            out Texture illumTex)
        {
            getTextures(
                diffusePath,
                normalPath,
                specularPath,
                illumPath,
                currentFolder,
                out diffuseTex,
                out normalTex,
                out specularTex,
                out illumTex
            );
        }

        public static void getTextures(
            string diffusePath, 
            string normalPath,
            string specularPath,
            string illumPath, 
            string currentFolder,
            out Texture diffuseTex, 
            out Texture normalTex,
            out Texture specularTex, 
            out Texture illumTex)
        {
            var texturesFolder = @"C:\png";

            if (Path.GetExtension(diffusePath) == ".mat")
            {
                var materialName = Path.GetFileName(diffusePath);
                var materialPath = Path.Combine(currentFolder, materialName);
                var material = Deserialize<Material>(materialPath);

                diffusePath = material.Textures.Diffuse?.Path ?? diffusePath;
                specularPath = material.Textures.Specular.Path ?? specularPath;
                illumPath = material.Textures.Illumination?.Path ?? illumPath;
            }

            diffuseTex = null;
            specularTex = null;
            illumTex = null;
            normalTex = null;

            var diffuseName = Path.GetFileNameWithoutExtension(diffusePath);
            diffusePath = Path.Combine(texturesFolder, diffuseName + ".png");
            if (File.Exists(diffusePath))
                diffuseTex = TextureCache.Get(diffusePath, PixelInternalFormat.CompressedRgba);

            var normName = Path.GetFileNameWithoutExtension(normalPath);
            var normPath = Path.Combine(texturesFolder, normName + ".png");
            if (File.Exists(normPath))
                normalTex = TextureCache.Get(normPath, PixelInternalFormat.CompressedRg);
            if (normalTex == null)
            {
                normPath = Path.Combine(texturesFolder, diffuseName + "_nrm.png");
                if (File.Exists(normPath))
                    normalTex = TextureCache.Get(normPath, PixelInternalFormat.CompressedRg);
            }


            var specName = Path.GetFileNameWithoutExtension(specularPath);
            specularPath = Path.Combine(texturesFolder, specName + ".png");
            if (File.Exists(specularPath)) {
                specularTex = TextureCache.Get(specularPath);
            } else {
                specularPath = Path.Combine(texturesFolder, diffuseName + "_spec.png");
                if (File.Exists(specularPath))
                    specularTex = TextureCache.Get(specularPath);
            }

            var name = Path.GetFileNameWithoutExtension(illumPath);
            illumPath = Path.Combine(texturesFolder, name + ".png");
            if (File.Exists(illumPath))
                illumTex = TextureCache.Get(illumPath, PixelInternalFormat.Rgb8);
            if (illumTex == null)
            {
                illumPath = illumPath.Replace("_emmi", "_illum");
                if (File.Exists(illumPath))
                    illumTex = TextureCache.Get(illumPath, PixelInternalFormat.Rgb8);
            }
            if (illumTex == null)
            {
                illumPath = Path.Combine(texturesFolder, diffuseName + "_illum.png");
                if (File.Exists(illumPath))
                    illumTex = TextureCache.Get(illumPath, PixelInternalFormat.Rgb8);
            }

            if (diffuseTex == null)
                diffuseTex = TextureCache.Get("assets/textures/error.png");
            if (normalTex == null)
                normalTex = TextureCache.Get("assets/textures/gray.png");
            if (specularTex == null)
                specularTex = TextureCache.Get("assets/textures/black.png");
            if (illumTex == null)
                illumTex = TextureCache.Get("assets/textures/black.png");
        }

        public new SOMAModel Clone() {
            return new SOMAModel(Renderables, Position, Rot, Scale);
        }

        public SOMAModel(List<Renderable> renderables) : this(renderables, Vector3.Zero, Quaternion.Identity, Vector3.One) { }
        private SOMAModel(List<Renderable> renderables, Vector3 pos, Quaternion rotation, Vector3 scale) : base(renderables, pos, rotation, scale) { 
        }
    }
}
