using Assimp;
using GLOOP.Extensions;
using GLOOP.Rendering;
using OpenTK;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace GLOOP.SOMA
{
    public class Map {
        private const string SOMARoot = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA";
        public List<SOMAModel> Models = new List<SOMAModel>();
        public List<GLOOP.PointLight> PointLights = new List<GLOOP.PointLight>();
        public List<GLOOP.SpotLight> SpotLights = new List<GLOOP.SpotLight>();

        public Map(string mapPath, AssimpContext assimp, DeferredRenderingGeoMaterial material) {
            loadLights(mapPath + "_Light");
            loadStaticObjects(mapPath + "_StaticObject", assimp, material);
            loadEntities(mapPath + "_Entity", assimp, material);
            loadDetailMeshes(mapPath + "_DetailMeshes", assimp, material);
            loadPrimitives(mapPath + "_Primitive", material);

            // Sort
            Models = Models
                .OrderByDescending(m => m.IsStatic)
                //.ThenByDescending(m => m.IsOccluder)
                .ThenBy(m => m.ResourcePath).ToList();
        }

        private void loadLights(string lightsPath)
        {
            var lightsModel = Deserialize<Lights>(lightsPath);

            var allLights = lightsModel
                .Sections
                .SelectMany(section => section.Lights);
            var spotLights = allLights
                .Where(l => l is SpotLight)
                .Cast<SpotLight>();
            var pointLights = allLights
                .Where(l => l is PointLight)
                .Cast<PointLight>();

            PointLights.AddRange(pointLights.Select(l => l.ToCommon()));
            SpotLights.AddRange(spotLights.Select(l => l.ToCommon()));
        }

        public void loadEntities(string entitiesFilePath, AssimpContext assimp, DeferredRenderingGeoMaterial material) {
            var entityDict = Deserialize<Entities>(entitiesFilePath);

            int attempted = 0, success = 0;
            var failed = new List<string>();
            var instances = new List<SOMAModel>();
            Console.WriteLine();
            Console.Write("Loading Entites");
            foreach (var section in entityDict.sections) {
                var files = new SOMAModel[section.Files.Length];
                var entities = new Entity[section.Files.Length];
                foreach (var entFile in section.Files) {
                    try {
                        attempted++;
                        var fullPath = Path.Combine(SOMARoot, entFile.Path);
                        Console.Write(".");

                        //if (entFile.Path.Contains("Generator_Habitat.ent")) { 
                        if (!entFile.Path.Contains("camera_surveillance")) {
                            var entity = Deserialize<Entity>(fullPath);
                            entities[entFile.Id] = entity;

                            var daePath = Path.Combine(SOMARoot, entity.Model.Mesh.FileName);
                            files[entFile.Id] = entity.Load(assimp, material);

                            //Console.WriteLine("SUCCESS.");
                            success++;
                        } else {
                            //Console.WriteLine("Skipped.");
                        }
                    } catch (Exception ex) {
                        //Console.WriteLine("Failed.");
                        //Console.WriteLine(ex);
                        failed.Add(entFile.Path);
                    }
                }

                foreach (var instance in section.Objects) {
                    var pos = instance.Position.ParseVector3();
                    var rot = new OpenTK.Mathematics.Quaternion(instance.Rotation.ParseVector3().Negated());
                    var scale = instance.Scale.ParseVector3();

                    var model = files[instance.Index];
                    var entity = entities[instance.Index];
                    if (model != null) {
                        var newInstance = model.Clone();
                        newInstance.Transform.Position += pos;
                        newInstance.Transform.Rotation += rot;
                        newInstance.Transform.Scale *= scale;

                        if (entity != null)
                        {
                            var variables = instance.GetProperties(entity);
                            var illuminationColor = new Vector3(1, 1, 1);
                            var brightness = 1f;

                            if (variables.TryGetValue("IllumColor", out var colorString))
                                illuminationColor = colorString.ParseVector4().Xyz;
                            if (variables.TryGetValue("IllumBrightness", out var brightnessString))
                                float.TryParse(brightnessString, out brightness);
                            
                            foreach (var renderable in newInstance.Renderables)
                            {
                                var mat = (DeferredRenderingGeoMaterial)renderable.Material;
                                mat.IlluminationColor = illuminationColor * brightness;
                            }
                        }

                        instances.Add(newInstance);
                    }
                    if (entity != null)
                    {
                        //var modelMatrix = MathFunctions.CreateModelMatrix(pos, rot, scale);
                        foreach (var light in entity.Model.Lights)
                        {
                            var l = light.ToCommon();
                            l.Position += pos;
                            PointLights.Add(l);
                        }
                    }
                }
            }

            Console.WriteLine($"Loaded {success} out of attempted {attempted}.");
            Console.WriteLine("Failed objects paths are as follows: ");
            foreach (var path in failed)
                Console.WriteLine(path);

            Models.AddRange(instances);
        }

        public void loadStaticObjects(string staticObjectsPath, AssimpContext assimp, DeferredRenderingGeoMaterial material) {
            var staticObjects = Deserialize<StaticObjects>(staticObjectsPath);

            int attempted = 0, success = 0;
            var failed = new List<string>();
            var instances = new List<SOMAModel>();
            Console.WriteLine();
            Console.Write("Loading Static Objects");
            foreach (var section in staticObjects.sections) {
                var files = new SOMAModel[section.Files.Length];
                foreach (var entFile in section.Files) {
                    try {
                        attempted++;
                        var fullPath = Path.Combine(SOMARoot, entFile.Path);
                        Console.Write(".");

                       //if (fullPath.Contains("05_01_adon_support.DAE") || fullPath.Contains("05_01_adon_box_small.DAE") || fullPath.Contains("phi_tunnel_straight.DAE")) { 
                        if (true) {
                            files[entFile.Id] = new SOMAModel(fullPath, assimp, material);

                            //Console.WriteLine("SUCCESS.");
                            success++;
                        } else {
                            //Console.WriteLine("Skipped.");
                        }
                    } catch (Exception ex) {
                        //Console.WriteLine("Failed.");
                        //Console.WriteLine(ex);
                        failed.Add(entFile.Path);
                    }
                }

                foreach (var instance in section.Objects) {
                    var pos = instance.Position.ParseVector3();
                    var rot = new OpenTK.Mathematics.Quaternion(instance.Rotation.ParseVector3().Negated());
                    var scale = instance.Scale.ParseVector3();

                    var model = files[instance.Index];
                    if (model != null) {
                        var newInstance = model.Clone();
                        newInstance.Transform.Position += pos;
                        newInstance.Transform.Rotation += rot;
                        newInstance.Transform.Scale *= scale;

                        var illumColor = instance.IlluminationColor?.ParseVector3() ?? Vector3.One;
                        var albedoTint = instance.ColourMultiplier?.ParseVector3() ?? Vector3.One;
                        foreach (var renderable in newInstance.Renderables)
                        {
                            var mat = (DeferredRenderingGeoMaterial)renderable.Material;
                            mat.AlbedoColourTint = albedoTint;
                            mat.IlluminationColor = illumColor * instance.IlluminationBrightness;
                        }

                        newInstance.IsStatic = true;
                        newInstance.IsOccluder = instance.IsOccluder;
                        instances.Add(newInstance);
                    }
                }
            }

            Console.WriteLine($"Loaded {success} out of attempted {attempted}.");
            Console.WriteLine("Failed objects paths are as follows: ");
            foreach (var path in failed)
                Console.WriteLine(path);

            Models.AddRange(instances);
        }

        private T Deserialize<T>(string path)
        {
            var primitivesSerializer = new XmlSerializer(typeof(T));
            using var file = File.OpenRead(path);
            return (T)primitivesSerializer.Deserialize(file);
        }

        public void loadPrimitives(string primitivesPath, DeferredRenderingGeoMaterial material)
        {
            var primitives = Deserialize<Primitives>(primitivesPath);

            int attempted = 0, success = 0;
            Console.WriteLine();
            Console.WriteLine("Loading primitives");
            foreach (var section in primitives.Sections) {
                foreach (var plane in section.Planes) {
                    try {
                        Console.Write(".");
                        attempted++;
                        var mat = Deserialize<Material>(Path.Combine(SOMARoot, plane.MaterialPath));

                        var endCorner = plane.Scale.ParseVector3();
                        var scale = new Vector2(endCorner.X, endCorner.Z);
                        var uvs = new Vector2[] {
                            plane.Corner1UV.ParseVector2(),
                            plane.Corner2UV.ParseVector2(),
                            plane.Corner3UV.ParseVector2(),
                            plane.Corner4UV.ParseVector2(),
                        };

                        var geo = GLOOP.Primitives.CreatePlane(scale, uvs);
                        var vao = geo.ToVirtualVAO(plane.Name);

                        SOMAModel.getTextures(
                            mat.Textures?.Diffuse?.Path,
                            mat.Textures?.NormalMap?.Path,
                            mat.Textures?.Specular?.Path,
                            null,
                            "",
                            out var diffuseTex,
                            out var normalTex,
                            out var specularTex,
                            out var illumTex
                        );

                        var materialInstance = (DeferredRenderingGeoMaterial)material.Clone();
                        materialInstance.TextureRepeat = plane.TileAmount.ParseVector2();
                        materialInstance.TextureOffset = plane.TileOffset.ParseVector2();
                        materialInstance.HasWorldpaceUVs = plane.AlignToWorldCoords;
                        materialInstance.DiffuseTexture = diffuseTex;
                        materialInstance.NormalTexture = normalTex;
                        materialInstance.SpecularTexture = specularTex;
                        materialInstance.IlluminationTexture = illumTex;

                        var renderable = new Model(Transform.Default, vao, materialInstance);
                        var model = new SOMAModel(new List<Model> { renderable }, vao.BoundingBox);
                        model.Transform.Position = plane.Position.ParseVector3();
                        //model.Rot += new OpenTK.Mathematics.Quaternion(plane.Rotation.ParseVector3().Negated());

                        Models.Add(model);
                        success++;
                    }
                    catch (Exception ex) { 

                    }
                }
            }

            Console.WriteLine($"Loaded {success} out of attempted {attempted}.");
        }

        public void loadDetailMeshes(string detailMeshesPath, AssimpContext assimp, DeferredRenderingGeoMaterial material)
        {
            var detailMeshesSerializer = new XmlSerializer(typeof(DetailMeshes));
            var detailMeshes = (DetailMeshes)detailMeshesSerializer.Deserialize(File.OpenRead(detailMeshesPath));

            int attempted = 0, success = 0;
            var failed = new List<string>();
            var instances = new List<SOMAModel>();
            Console.WriteLine();
            Console.Write("Loading Details");
            foreach (var section in detailMeshes.meshes.sections) {
                if (section.DetailMeshes == null)
                    continue;
                foreach (var detailMesh in section.DetailMeshes) {
                    try {
                        attempted++;
                        var fullPath = Path.Combine(SOMARoot, detailMesh.FilePath);
                        Console.Write(".");

                        if (true) {
                            var model = new SOMAModel(fullPath, assimp, material);
                            var positions = detailMesh.GetPositions().ToArray();
                            var rotations = detailMesh.GetRotations().ToArray();

                            for (var i=0; i<detailMesh.NumInstances; i++) {
                                var newInstance = model.Clone();
                                newInstance.Transform.Position += positions[i];
                                newInstance.Transform.Rotation += rotations[i];

                                instances.Add(newInstance);
                            }

                            success++;
                        }
                        else {
                            Console.WriteLine("Skipped.");
                        }
                    }
                    catch (Exception ex) {
                        //Console.WriteLine("Failed.");
                        //Console.WriteLine(ex);
                        failed.Add(detailMesh.FilePath);
                    }
                }
            }

            Console.WriteLine($"Loaded {success} out of attempted {attempted}.");
            Console.WriteLine("Failed objects paths are as follows: ");
            foreach (var path in failed)
                Console.WriteLine(path);

            Models.AddRange(instances);
        }

        public void Render(Matrix4 projectionMatrix, Matrix4 viewMatrix)
        {
            foreach (var model in Models)
            {
                model.Render(projectionMatrix, viewMatrix);
               // model.RenderBoundingBox(projectionMatrix, viewMatrix);
            }
        }
    }
}
