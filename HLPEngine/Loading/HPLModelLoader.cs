using GLOOP.Rendering;
using GLOOP.HPL;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using GLOOP.Rendering.Materials;
using HPLEngine.Loading;

namespace GLOOP.HPL.Loading
{
    public class HPLModelLoader : ModelLoader {
        public static Vector3 Offset = new Vector3(0,0,0);

        public string ResourcePath { get; }

        public HPLModelLoader(string path,  Assimp.AssimpContext assimp, DeferredRenderingGeoMaterial material) 
            : base(path, assimp, material) {
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
            string sourceFile,
            out Texture2D diffuseTex,
            out Texture2D normalTex,
            out Texture2D specularTex,
            out Texture2D illumTex)
        {
            getTextures(
                diffusePath,
                normalPath,
                specularPath,
                illumPath,
                sourceFile,
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
            string sourceFile,
            out Texture2D diffuseTex, 
            out Texture2D normalTex,
            out Texture2D specularTex, 
            out Texture2D illumTex)
        {
            diffuseTex = null;
            specularTex = null;
            illumTex = null;
            normalTex = null;

            if (string.IsNullOrEmpty(diffusePath))
                diffusePath = Path.ChangeExtension(sourceFile, "dds");
            if (Path.GetExtension(diffusePath) != ".dds")
                diffusePath = Path.ChangeExtension(diffusePath, "dds");
            
            try
            {
                var diffuseName = Path.GetFileName(diffusePath);
                
                if (Stores.MAT.TryGetValue(Path.ChangeExtension(diffuseName, ".mat"), out var materialPath))
                {
                    var material = Deserialize<Material>(materialPath);

                    diffusePath = material.Textures.Diffuse?.Path ?? diffusePath;
                    specularPath = material.Textures.Specular?.Path ?? specularPath;
                    illumPath = material.Textures.Illumination?.Path ?? illumPath;

                    diffuseName = Path.GetFileName(diffusePath);
                }

                if (Stores.DDS.TryGetValue(diffuseName, out diffusePath))
                    diffuseTex = TextureCache.Get(diffusePath, PixelInternalFormat.CompressedRgbaS3tcDxt5Ext);

                static Texture2D TryFindTex(string[] names, PixelInternalFormat format)
                {
                    Texture2D tex = null;
                    foreach (var name in names)
                    {
                        if (Stores.DDS.TryGetValue(name + ".dds", out var path))
                        {
                            tex = TextureCache.Get(path, format);
                            if (tex != null)
                               break;
                        }
                    }

                    return tex;
                }

                diffuseName = Path.GetFileNameWithoutExtension(diffusePath);
                var normNames = new[] {
                    Path.GetFileNameWithoutExtension(normalPath ?? ""),
                    diffuseName + "_nrm",
                    diffuseName + "_norm",
                    diffuseName + "_norm",
                    diffuseName + "_nmor",
                };
                normalTex = TryFindTex(normNames, PixelInternalFormat.CompressedRg);

                var specNames = new[]
                {
                    Path.GetFileNameWithoutExtension(specularPath ?? ""),
                    diffuseName + "_spec"
                };
                specularTex = TryFindTex(specNames, PixelInternalFormat.Rgba);

                var illumNames = new[]
                {
                    Path.GetFileNameWithoutExtension(illumPath ?? ""),
                    diffuseName + "_illum",
                    diffuseName + "_emmi"
                };
                illumTex = TryFindTex(illumNames, PixelInternalFormat.Rgb8);
            } 
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            if (diffuseTex == null)
                diffuseTex = Texture.Error;
            if (normalTex == null)
                normalTex = Texture.Gray;
            if (specularTex == null)
                specularTex = Texture.Black;
            if (illumTex == null)
                illumTex = Texture.Black;
        }

        public new HPLModelLoader Clone() {
            return new HPLModelLoader(Models, Transform.Clone(), OriginalBoundingBox);
        }

        public HPLModelLoader(List<Model> renderables, Box3 boundingBox) 
            : this(renderables.Select(r => r.Clone()).ToList(), DynamicTransform.Default, boundingBox) { }
        private HPLModelLoader(List<Model> renderables, DynamicTransform transform, Box3 originalBoundingBox) 
            : base(renderables.Select(r => r.Clone()).ToList(), transform, originalBoundingBox) { 
        }
    }
}
