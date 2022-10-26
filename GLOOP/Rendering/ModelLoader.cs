using GLOOP.Extensions;
using GLOOP.Rendering.Materials;
using GLOOP.Util;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GLOOP.Rendering
{
    public class ModelLoader
    {
        public DynamicTransform Transform = DynamicTransform.Default;
        public bool IsStatic = false;
        public bool IsOccluder = false;
        public readonly Box3 OriginalBoundingBox = new Box3();
        public Box3 BoundingBox
        {
            get
            {
                //var scaledBB = OriginalBoundingBox
                //    .Translated(-OriginalBoundingBox.Center)
                //    .Scaled(Scale, Vector3.Zero)
                //    .Rotated(Rot.Inverted());
                var modelMatrix = MathFunctions.CreateModelMatrix(Vector3.Zero, Transform.Rotation, Transform.Scale);
                var result = OriginalBoundingBox.Transform(Transform.Matrix);
                return result;
            }
        }
        public Vector3 WorldScale => new Vector3(
                    Transform.Scale.X * OriginalBoundingBox.Size.X,
                    Transform.Scale.Y * OriginalBoundingBox.Size.Y,
                    Transform.Scale.Z * OriginalBoundingBox.Size.Z
                );

        public List<Model> Models = new List<Model>();

        public ModelLoader(List<Model> renderables, Box3 boundingBox) 
            : this(renderables, DynamicTransform.Default, boundingBox) { }
        protected ModelLoader(List<Model> renderables, DynamicTransform transform, Box3 originalBoundingBox)
        {
            Models = renderables;
            Transform = transform;
            OriginalBoundingBox = originalBoundingBox;
        }
        public ModelLoader(string path, Assimp.AssimpContext assimp, Material material)
        {
            if (Path.GetExtension(path).ToLower() == ".fbx")
                throw new NotSupportedException("FBX files not supported yet");

            var steps = Assimp.PostProcessSteps.FlipUVs
                | Assimp.PostProcessSteps.GenerateNormals
                | Assimp.PostProcessSteps.CalculateTangentSpace
                | Assimp.PostProcessSteps.PreTransformVertices
                | Assimp.PostProcessSteps.Triangulate;
            var startLoadingModel = DateTime.Now;
            var scene = assimp.ImportFile(path, steps);
            Metrics.TimeLoadingModels += DateTime.Now - startLoadingModel;
            if (!scene.HasMeshes)
                return;

            /*
            var root = scene.RootNode;
            root.Transform.Decompose(out var scale, out var rotation, out var position);
            Transform.Position += new Vector3(position.X, position.Y, position.Z);
            Transform.Scale *= new Vector3(scale.X, scale.Y, scale.Z);
            Transform.Rotation *= new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
            Transform.Rotation.Invert();
            */
            /*
            if (Path.GetExtension(path).ToLower() == ".dae")
            {
                var daeScale = DAE.Model.Load(path)?.Meta?.Units?.Scale;
                float? daeScale = daeScaleName?.ToLower() switch
                {
                    "meter" => 1f,
                    "centimeter" => 0.1f,
                    "millimeter" => 0.01f,
                    _ => null
                };
                Transform.Scale *= daeScale ?? 1.0f;
            }
            */

            var currentFolder = Path.GetDirectoryName(path);

            for (var i = 0; i < scene.Meshes.Count; i++)
            {
                var mesh = scene.Meshes[i];
                var mat = scene.Materials[mesh.MaterialIndex];
                string diffTex = mat.TextureDiffuse.FilePath;
                string normTex = mat.TextureNormal.FilePath;
                string specTex = mat.TextureSpecular.FilePath;
                string emmtex = mat.TextureEmissive.FilePath;
                foreach (var m in scene.Materials)
                {
                    diffTex ??= m.TextureDiffuse.FilePath;
                    normTex ??= m.TextureNormal.FilePath;
                    specTex ??= m.TextureSpecular.FilePath;
                    emmtex ??= m.TextureEmissive.FilePath;
                }
                var beforeLoadingTextures = DateTime.Now;
                GetTextures(
                    diffTex,
                    normTex,
                    specTex,
                    emmtex,
                    path,
                    out var diffuseTex,
                    out var normalTex,
                    out var specularTex,
                    out var illumTex
                );
                Metrics.TimeLoadingTextures += DateTime.Now - beforeLoadingTextures;

                var startSavingModel = DateTime.Now;
                var vaoName = $"{path}[{i}]";
                VirtualVAO vao;
                if (!VAOCache.Get(vaoName, out vao))
                {
                    var geo = new Geometry();
                    geo.Positions = mesh.Vertices.Select(v => v.ToOpenTK()).ToList();
                    if (!mesh.HasTextureCoords(0))
                        throw new Exception("No texture coords");
                    geo.UVs = mesh.TextureCoordinateChannels[0].Select(uv => new Vector2(uv.X, uv.Y)).ToList();
                    geo.Indicies = mesh.GetIndices().Cast<uint>().ToList();
                    geo.Normals = mesh.Normals.Select(n => n.ToOpenTK()).ToList();
                    if (mesh.HasTangentBasis)
                        geo.Tangents = mesh.Tangents.Select(n => n.ToOpenTK()).ToList();
                    else
                        geo.CalculateTangents();
                    //geo.Scale(new Vector3(Transform.Scale.X, Transform.Scale.Y, Transform.Scale.Z));

                    vao = geo.ToVirtualVAO();
                    VAOCache.Put(vao, vaoName);
                }

                OriginalBoundingBox = OriginalBoundingBox.Union(vao.BoundingBox);

                var materialInstance = material.Clone();
                materialInstance.SetTextures(diffuseTex, normalTex, specularTex, illumTex);
                Models.Add(new Model(vao, materialInstance));

                Metrics.TimeLoadingModels += DateTime.Now - startSavingModel;
            }

            Transform.Scale = Vector3.One;
        }

        public virtual void GetTextures(
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

            if (File.Exists(diffusePath))
                diffuseTex = TextureCache.Get(diffusePath);

            if (File.Exists(specularPath))
                specularTex = TextureCache.Get(specularPath);

            if (File.Exists(illumPath))
                illumTex = TextureCache.Get(illumPath);

            if (File.Exists(normalPath))
                normalTex = TextureCache.Get(normalPath);

            if (diffuseTex == null)
                diffuseTex = Texture.Error;
            if (normalTex == null)
                normalTex = Texture.Gray;
            if (specularTex == null)
                specularTex = Texture.Black;
            if (illumTex == null)
                illumTex = Texture.Black;
        }

        public void Render()
        {
            foreach (var renderable in Models)
                renderable.Render();
        }

        public ModelLoader Clone() => new ModelLoader(Models, Transform, OriginalBoundingBox);
    }
}
