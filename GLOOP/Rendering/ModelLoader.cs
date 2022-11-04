using Assimp;
using GLOOP.Animation;
using GLOOP.Extensions;
using GLOOP.Rendering.Materials;
using GLOOP.Util;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public ModelLoader(string modelPath, AssimpContext assimp, Material material)
        {
            if (Path.GetExtension(modelPath).ToLower() == ".fbx")
                throw new NotSupportedException("FBX files not supported yet");

            var steps = 
                  PostProcessSteps.FlipUVs
                | PostProcessSteps.PreTransformVertices
                | PostProcessSteps.GenerateNormals
                | PostProcessSteps.CalculateTangentSpace
                //| PostProcessSteps.LimitBoneWeights
                | PostProcessSteps.Triangulate;
            var startLoadingModel = DateTime.Now;
            var assimpScene = assimp.ImportFile(modelPath, steps);
            Metrics.TimeLoadingModels += DateTime.Now - startLoadingModel;
            if (!assimpScene.HasMeshes)
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

            var currentFolder = Path.GetDirectoryName(modelPath);

            for (var i = 0; i < assimpScene.Meshes.Count; i++)
            {
                var assimpMesh = assimpScene.Meshes[i];
                var assimpMat = assimpScene.Materials[assimpMesh.MaterialIndex];
                string diffTex = assimpMat.TextureDiffuse.FilePath;
                string normTex = assimpMat.TextureNormal.FilePath;
                string specTex = assimpMat.TextureSpecular.FilePath;
                string emmtex = assimpMat.TextureEmissive.FilePath;
                foreach (var m in assimpScene.Materials)
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
                    modelPath,
                    out var diffuseTex,
                    out var normalTex,
                    out var specularTex,
                    out var illumTex
                );
                Metrics.TimeLoadingTextures += DateTime.Now - beforeLoadingTextures;

                var startSavingModel = DateTime.Now;

                VirtualVAO vao;
                Skeleton skeleton = null;
                SkeletonAnimationSet animationSet = null;

                var vaoName = $"{modelPath}[{i}]";
                if (!VAOCache.Get(vaoName, out vao))
                {
                    var geo = new Geometry();
                    geo.Positions = assimpMesh.Vertices.Select(v => v.ToOpenTK()).ToList();
                    if (!assimpMesh.HasTextureCoords(0))
                        throw new Exception("No texture coords");
                    geo.UVs = assimpMesh.TextureCoordinateChannels[0].Select(uv => new Vector2(uv.X, uv.Y)).ToList();
                    geo.Indicies = assimpMesh.GetIndices().Cast<uint>().ToList();
                    geo.Normals = assimpMesh.Normals.Select(n => n.ToOpenTK()).ToList();
                    if (assimpMesh.HasTangentBasis)
                        geo.Tangents = assimpMesh.Tangents.Select(n => n.ToOpenTK()).ToList();
                    else
                        geo.CalculateTangents();
                    //geo.Scale(new Vector3(Transform.Scale.X, Transform.Scale.Y, Transform.Scale.Z));

                    var animFolder = Path.GetDirectoryName(modelPath);
                    animFolder = Path.Combine(animFolder, "animations");
                    if (Directory.Exists(animFolder))
                    {
                        var assimpSkeletonScene = assimp.ImportFile(modelPath, PostProcessSteps.LimitBoneWeights);
                        var allBoneNames = assimpSkeletonScene.Meshes[i].Bones.Select(x => x.Name).ToList();
                        var animModel = assimpSkeletonScene.Meshes[0];
                        if (assimpSkeletonScene.MeshCount == 1 && animModel.HasBones)
                        {
                            var vertcies = CreateVertexWeights(assimpMesh.Bones, geo.Positions.Count);
                            geo.BoneIds = vertcies.SelectMany(p => p.Ids).ToVec4s().ToList();
                            geo.BoneWeights = vertcies.SelectMany(p => p.Weights).ToVec4s().ToList();

                            skeleton = new Skeleton(
                                assimpSkeletonScene.RootNode.Find(b => allBoneNames.Contains(b.Name)),
                                assimpSkeletonScene.Meshes[i].Bones
                            );
                            animationSet = LoadAnimations(
                                skeleton,
                                assimp,
                                animFolder
                            );
                        }
                    }

                    vao = geo.ToVirtualVAO();
                    VAOCache.Put(vao, vaoName);
                }

                OriginalBoundingBox = OriginalBoundingBox.Union(vao.BoundingBox);

                var materialInstance = material.Clone();
                materialInstance.SetTextures(diffuseTex, normalTex, specularTex, illumTex);
                var model = new Model(vao, materialInstance);
                if (skeleton != null && animationSet.Any())
                {
                    model.Skeleton = skeleton;
                    model.Animations = animationSet;
                    model.AnimationDriver = new SkeletonAnimationDriver(skeleton);
                    Transform.Scale /= 100;
                }
                Models.Add(model);

                Metrics.TimeLoadingModels += DateTime.Now - startSavingModel;
            }

            //Transform.Scale = Vector3.One;
        }

        private SkeletonAnimationSet LoadAnimations(
            Skeleton skeleton,
            AssimpContext assimp,
            string animFolder)
        {
            var animationSet = new SkeletonAnimationSet(10);

            foreach (var animFilePath in Directory.EnumerateFiles(animFolder, "*.dae_anim"))
            {
                var assimpScene = assimp.ImportFile(animFilePath, PostProcessSteps.LimitBoneWeights);
                var animations = new SkeletonAnimationSet(4);
                foreach (var anim in assimpScene.Animations)
                    animations.Add(
                        new SkeletonAnimation(
                            anim.NodeAnimationChannels,
                            skeleton.AllBones,
                            anim.Name,
                            (float)anim.TicksPerSecond
                        )
                    );
                animations.MergeAllAs(Path.GetFileName(animFilePath));
                animationSet.Add(animations[0]);

                Console.WriteLine($"Loaded animation '{animFilePath}'");
            }

            return animationSet;
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

        public ModelLoader Clone() => new ModelLoader(Models, Transform.Clone(), OriginalBoundingBox);

        // TODO: Ids are ints not floats
        private VertexBoneData[] CreateVertexWeights(IEnumerable<Assimp.Bone> bones, int numVerts)
        {

            const float fillerValue = float.MaxValue;
            var vertcies = Enumerable.Range(0, numVerts)
                .Select(i => new VertexBoneData(
                    Enumerable.Repeat(fillerValue, 4).ToArray(),
                    Enumerable.Repeat(fillerValue, 4).ToArray()
                )
            ).ToArray();
            int boneIdx = 0;
            foreach (var bone in bones)
            {
                Debug.Assert(bone.HasVertexWeights);

                foreach (var weight in bone.VertexWeights)
                {
                    var nextBlankSlot = vertcies[weight.VertexID].Ids.IndexOf(fillerValue);
                    vertcies[weight.VertexID].Ids[nextBlankSlot] = boneIdx++;
                    nextBlankSlot = vertcies[weight.VertexID].Weights.IndexOf(fillerValue);
                    vertcies[weight.VertexID].Weights[nextBlankSlot] = weight.Weight;
                }
            }

            foreach (var vert in vertcies)
            {
                var sum = vert.Weights.Where(x => x != fillerValue).Sum();
                ///Debug.Assert(sum > 0.99999f && sum < 1.0001);
                for (int i = 0; i < 4; i++)
                    if (vert.Weights[i] != float.MaxValue)
                        Debug.Assert(vert.Ids[i] != float.MaxValue, "weight linking to no bone");
            }

            // Cleanup
            foreach (var vert in vertcies)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (vert.Ids[i] == fillerValue) vert.Ids[i] = 0;
                    if (vert.Weights[i] == fillerValue) vert.Weights[i] = 0;
                }
            }

            return vertcies;
        }

        private readonly struct VertexBoneData
        {
            public readonly float[] Ids;
            public readonly float[] Weights;

            public VertexBoneData(float[] boneIds, float[] boneWeights)
            {
                Ids = boneIds;
                Weights = boneWeights;
            }
        }
    }
}
