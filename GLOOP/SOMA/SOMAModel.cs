﻿using GLOOP.Rendering;
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
            diffuseTex = null;
            specularTex = null;
            illumTex = null;
            normalTex = null;

            try
            {
                var extension = "png";
                var texturesFolder = @"C:\" + extension;
                Texture findTex(string[] names, PixelInternalFormat format)
                {
                    Texture tex = null;

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

                if (Path.GetExtension(diffusePath) == ".mat")
                {
                    var materialName = Path.GetFileName(diffusePath);
                    var materialPath = Path.Combine(@"c:\mat", materialName);
                    var material = Deserialize<Material>(materialPath);

                    diffusePath = material.Textures.Diffuse?.Path ?? diffusePath;
                    specularPath = material.Textures.Specular?.Path ?? specularPath;
                    illumPath = material.Textures.Illumination?.Path ?? illumPath;
                }

                diffusePath = Path.ChangeExtension(diffusePath, extension);
                normalPath ??= diffusePath;
                specularPath ??= diffusePath;

                var diffuseName = Path.GetFileNameWithoutExtension(diffusePath);
                diffusePath = Path.Combine(texturesFolder, Path.GetFileName(diffusePath));
                if (File.Exists(diffusePath))
                    diffuseTex = TextureCache.Get(diffusePath, PixelInternalFormat.CompressedRgbaS3tcDxt5Ext);

                var normNames = new[] {
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
                specularTex = findTex(specNames, PixelInternalFormat.Rgb8);
            } 
            catch (Exception ex)
            {
                Console.WriteLine(ex);
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
