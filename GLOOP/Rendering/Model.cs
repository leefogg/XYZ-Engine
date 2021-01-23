using GLOOP.Extensions;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GLOOP.Rendering
{
    public class Model
    {
        public Vector3 Position = new Vector3(0, 0, 0);
        public Vector3 Scale = new Vector3(1, 1, 1);
        public Quaternion Rot = new Quaternion();
        public bool IsStatic = false;
        public bool IsOccluder = false;
        public readonly Box3 OriginalBoundingBox = new Box3();
        public Box3 BoundingBox
        {
            get
            {
                var scaledBB = OriginalBoundingBox.Scaled(Scale, OriginalBoundingBox.Center);
                return scaledBB.Rotated(Rot.Inverted());
            }
        }
        public Vector3 WorldScale => new Vector3(
                    Scale.X * OriginalBoundingBox.Size.X,
                    Scale.Y * OriginalBoundingBox.Size.Y,
                    Scale.Z * OriginalBoundingBox.Size.Z
                );

        public List<Renderable> Renderables = new List<Renderable>();

        public Model(string path, Assimp.AssimpContext assimp, Material material)
        {
            var steps = Assimp.PostProcessSteps.FlipUVs
                | Assimp.PostProcessSteps.PreTransformVertices
                | Assimp.PostProcessSteps.GenerateNormals
                | Assimp.PostProcessSteps.CalculateTangentSpace
                | Assimp.PostProcessSteps.Triangulate;
            var scene = assimp.ImportFile(path, steps);
            if (!scene.HasMeshes)
                return;

            var root = scene.RootNode;
            root.Transform.Decompose(out var scale, out var rotation, out var position);
            Position += new Vector3(position.X, position.Y, position.Z);
            Scale *= new Vector3(scale.X, scale.Y, scale.Z);
            Rot *= new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
            Rot.Invert();

            var daeScale = DAE.Model.Load(path)?.Meta?.Units?.Scale;
            Scale *= daeScale ?? 1.0f;

            var currentFolder = Path.GetDirectoryName(path);

            for (var i = 0; i < scene.Meshes.Count; i++)
            {
                var mesh = scene.Meshes[i];

                var mat = scene.Materials[mesh.MaterialIndex];
                var beforeLoadingTextures = DateTime.Now;
                GetTextures(
                    mat.TextureDiffuse.FilePath,
                    mat.TextureNormal.FilePath,
                    mat.TextureSpecular.FilePath,
                    mat.TextureEmissive.FilePath,
                    currentFolder,
                    out var diffuseTex,
                    out var normalTex,
                    out var specularTex,
                    out var illumTex
                );
                Metrics.TimeLoadingTextures += DateTime.Now - beforeLoadingTextures;

                var vaoName = $"{path}[{i}]";
                VirtualVAO vao;
                if (!VAOCache.Get(vaoName, out vao))
                {
                    var geo = new Geometry();
                    geo.Positions = mesh.Vertices.Select(v => new Vector3(v.X, v.Y, v.Z)).ToList();
                    if (!mesh.HasTextureCoords(0))
                        throw new Exception("No texture coords");
                    geo.UVs = mesh.TextureCoordinateChannels[0].Select(uv => new Vector2(uv.X, uv.Y)).ToList();
                    geo.Indicies = new List<int>(mesh.GetIndices());
                    geo.Normals = mesh.Normals.Select(n => new Vector3(n.X, n.Y, n.Z)).ToList();
                    if (mesh.HasTangentBasis)
                        geo.Tangents = mesh.Tangents.Select(n => new Vector3(n.X, n.Y, n.Z)).ToList();
                    else
                        geo.CalculateTangents();
                    geo.Scale(new Vector3(Scale.X, Scale.Y, Scale.Z));

                    OriginalBoundingBox = OriginalBoundingBox.Union(geo.GetBoundingBox());

                    vao = geo.ToVirtualVAO(vaoName);
                    VAOCache.Put(vao, vaoName);
                }

                var materialInstance = material.Clone();
                materialInstance.SetTextures(diffuseTex, normalTex, specularTex, illumTex);
                Renderables.Add(new Renderable(vao, materialInstance));
            }

            Scale = new Vector3(1);
            var bbSize = OriginalBoundingBox.Size;
        }

        public virtual void GetTextures(
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

            if (File.Exists(diffusePath))
                diffuseTex = TextureCache.Get(diffusePath);

            if (File.Exists(specularPath))
                specularTex = TextureCache.Get(specularPath);

            if (File.Exists(illumPath))
                illumTex = TextureCache.Get(illumPath);

            if (File.Exists(normalPath))
                normalTex = TextureCache.Get(normalPath);

            if (diffuseTex == null)
                diffuseTex = TextureCache.Get("assets/textures/error.png");
            if (normalTex == null)
                normalTex = TextureCache.Get("assets/textures/gray.png");
            if (specularTex == null)
                specularTex = TextureCache.Get("assets/textures/black.png");
            if (illumTex == null)
                illumTex = TextureCache.Get("assets/textures/black.png");
        }

        public void Render(Matrix4 projectionMatrix, Matrix4 viewMatrix)
        {
            var modelMatrix = MathFunctions.CreateModelMatrix(Position, Rot, Scale); // TODO: This should be cached

            foreach (var renderable in Renderables)
                renderable.Render(projectionMatrix, viewMatrix, modelMatrix);
        }

        public Model Clone() => new Model(Renderables, Position, Rot, Scale);

        public Model(List<Renderable> renderables) : this(renderables, Vector3.Zero, Quaternion.Identity, Vector3.One) { }
        public Model(List<Renderable> renderables, Vector3 pos, Quaternion rotation, Vector3 scale)
        {
            Renderables = renderables;
            Position = pos;
            Rot = rotation;
            Scale = scale;
        }
    }
}
