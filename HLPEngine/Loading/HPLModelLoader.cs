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

            try
            {
                var extension = "dds";
                var texturesFolder = Constants.RootFolder + extension;
                Texture2D findTex(string[] names, PixelInternalFormat format)
                {
                    Texture2D tex = null;

                    foreach (var name in names)
                    {
                        var path = Path.Combine(texturesFolder, name + "." + extension);
                        if (!File.Exists(path))
                            continue;
                        tex = TextureCache.Get(path, format);
                        if (tex != null)
                            break;
                    }

                    return tex;
                }

                string materialPath = null;
                if (Path.GetExtension(diffusePath) == ".mat")
                {
                    var matPath = Path.Combine(Constants.MaterialsFolder, Path.GetFileName(diffusePath));
                    if (File.Exists(matPath))
                        materialPath = matPath;
                }
                if (materialPath == null)
                {
                    var materialName = Path.GetFileName(diffusePath);
                    var matPath = Path.Combine(Constants.MaterialsFolder, Path.ChangeExtension(materialName, "mat"));
                    if (File.Exists(matPath))
                        materialPath = matPath;
                }
                if (materialPath != null)
                {
                    var material = Deserialize<Material>(materialPath);

                    diffusePath = material.Textures.Diffuse?.Path ?? diffusePath;
                    specularPath = material.Textures.Specular?.Path ?? specularPath;
                    illumPath = material.Textures.Illumination?.Path ?? illumPath;
                }

                diffusePath = Path.ChangeExtension(diffusePath, extension);

                string diffuseName;
                if (diffusePath != null)
                {
                    diffuseName = Path.GetFileNameWithoutExtension(diffusePath);
                    diffusePath = Path.Combine(texturesFolder, Path.GetFileName(diffusePath));
                    if (File.Exists(diffusePath))
                        diffuseTex = TextureCache.Get(diffusePath, PixelInternalFormat.CompressedRgbaS3tcDxt5Ext);
                } 
                else
                {
                    diffuseTex = Texture.Error;
                    diffuseName = string.Empty;
                }

                var normNames = new[] {
                    Path.GetFileNameWithoutExtension(normalPath ?? ""),
                    diffuseName + "_nrm",
                    diffuseName + "_norm",
                    diffuseName + "_norm",
                    diffuseName + "_nmor",
                };
                normalTex = findTex(normNames, PixelInternalFormat.CompressedRg);

                var specNames = new[]
                {
                    Path.GetFileNameWithoutExtension(specularPath ?? ""),
                    diffuseName + "_spec"
                };
                specularTex = findTex(specNames, PixelInternalFormat.Rgba);

                var illumNames = new[]
                {
                    Path.GetFileNameWithoutExtension(illumPath ?? ""),
                    diffuseName + "_illum",
                    diffuseName + "_emmi"
                };
                illumTex = findTex(illumNames, PixelInternalFormat.Rgb8);
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
            return new HPLModelLoader(Models, Transform, OriginalBoundingBox);
        }

        public HPLModelLoader(List<Model> renderables, Box3 boundingBox) 
            : this(renderables.Select(r => r.Clone()).ToList(), DynamicTransform.Default, boundingBox) { }
        private HPLModelLoader(List<Model> renderables, DynamicTransform transform, Box3 originalBoundingBox) 
            : base(renderables.Select(r => r.Clone()).ToList(), transform, originalBoundingBox) { 
        }
    }
}
