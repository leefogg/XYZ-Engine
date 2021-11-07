using Assimp;
using GLOOP.Extensions;
using GLOOP.Rendering;
using GLOOP.Rendering.Materials;
using OpenTK;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace GLOOP.HPL.Loading
{
    public class Map {
        private const string SOMARoot = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA";
        public List<HPLEntity> Entities = new List<HPLEntity>();
        public List<Model> Models = new List<Model>();
        public List<Model> Terrain = new List<Model>();
        public List<GLOOP.PointLight> PointLights = new List<GLOOP.PointLight>();
        public List<GLOOP.SpotLight> SpotLights = new List<GLOOP.SpotLight>();
        public Areas Areas;

        public Map(string mapPath, AssimpContext assimp, DeferredRenderingGeoMaterial material) {
            loadAreas(mapPath + "_Area");
            loadLights(mapPath + "_Light");

            loadStaticObjects(mapPath + "_StaticObject", assimp, material);
            //loadEntities(mapPath + "_Entity", assimp, material);
            //loadDetailMeshes(mapPath + "_DetailMeshes", assimp, material);
            //loadPrimitives(mapPath + "_Primitive", material);
            //loadTerrain(mapPath);
        }

        private void loadAreas(string areaFilePath)
        {
            Areas = Deserialize<Areas>(areaFilePath);
        }

        private void loadTerrain(string baseFilePath)
        {
            var heightmapInfoPath = baseFilePath + "_Terrain";
            var terrainInfo = Deserialize<Terrain>(heightmapInfoPath).Info;
            if (!terrainInfo.Active || string.IsNullOrEmpty(terrainInfo.BaseMaterialFile))
                return;

            var baseMaterial = Deserialize<Material>(Constants.MaterialsFolder + Path.GetFileName(terrainInfo.BaseMaterialFile));
            var baseTexture = TextureCache.Get(Constants.TexturesDDSFolder + Path.GetFileName(baseMaterial.Textures.Diffuse.Path));

            var splatTexture = new Texture2D(baseFilePath + "_Terrain_blendlayer_0.dds");

            var textures = new Texture2D[4];
            for (int i = 0; i < 4; i++)
            {
                if (string.IsNullOrEmpty(terrainInfo.BlendLayers.Materials[i].File))
                {
                    textures[i] = Texture.Black;
                } 
                else
                {
                    var materialPath = Constants.MaterialsFolder + Path.GetFileName(terrainInfo.BlendLayers.Materials[i].File);
                    var material = Deserialize<Material>(materialPath);
                    var texturePath = Constants.TexturesDDSFolder + Path.GetFileName(material.Textures.Diffuse.Path);
                    textures[i] = TextureCache.Get(texturePath);
                }
            }

            var terrainShader = new DeferredSplatTerrainShader();

            var heightmapPath = baseFilePath + "_Terrain_heightmap.dds";
            var pixelData = IO.DDSImage.GetPixelData(heightmapPath, out var width, out var height);

            // Extract heightmap
            var heightmap = new byte[pixelData.Length / 3];
            for (int i = 2; i < pixelData.Length; i += 3)
                heightmap[i / 3] = pixelData[i];

            var terrainResolution = new Vector2i(width, height);
            var chunkResolution = new Vector2i(128);
            var numChunks = new Vector2i(terrainResolution.X / chunkResolution.X, terrainResolution.Y / chunkResolution.Y);
            var terrainSize = new Vector2(terrainInfo.HeightMapSize);
            var halfTerrainSize = new Vector3(terrainSize.X / 2, 0, terrainSize.Y / 2);
            var chunkSize = new Vector3(terrainSize.X / numChunks.X, terrainInfo.MaxHeight, terrainSize.Y / numChunks.Y);
            var halfChunkSize = new Vector3(chunkSize.X / 2, 0, chunkSize.Y / 2);
            var chunkUVScale = new Vector2(1f / numChunks.X, 1f / numChunks.Y);
            var tileAmounts = new Vector4(
                terrainInfo.BlendLayers.Materials[0].TileAmount / numChunks.X,
                terrainInfo.BlendLayers.Materials[1].TileAmount / numChunks.X,
                terrainInfo.BlendLayers.Materials[2].TileAmount / numChunks.X,
                terrainInfo.BlendLayers.Materials[3].TileAmount / numChunks.X
            );
            int totalChunks = numChunks.X * numChunks.Y;

            VAOManager.Create(
                new VAO.VAOShape(true, true, true, false),
                (chunkResolution.X-1) * (chunkResolution.Y-1) * totalChunks, 
                chunkResolution.X * chunkResolution.Y * totalChunks
            );

            for (int z = 0; z < numChunks.Y; z++)
            {
                for (int x = 0; x < numChunks.X; x++)
                {
                    var chunkPosition = new Vector2i(x, z);
                    var chunk = GLOOP.Primitives.CreatePlane(chunkResolution.X-1, chunkResolution.Y-1);
                    chunk.UVs = chunk.UVs.Select(uv => uv * chunkUVScale + chunkUVScale * new Vector2(chunkPosition.Y, chunkPosition.X)).Select(uv => new Vector2(uv.Y, uv.X)).ToList();
                    var minUV = chunk.UVs[0];
                    var maxUV = chunk.UVs[^1];

                    var startPixelX = chunkPosition.X * chunkResolution.X;
                    var startPixelY = chunkPosition.Y * chunkResolution.Y;
                    startPixelX -= chunkPosition.X;
                    startPixelY -= chunkPosition.Y;

                    var pixel = startPixelY * terrainResolution.X + startPixelX;
                    for (int py = 0; py < chunkResolution.Y; py++)
                    {
                        for (int px = 0; px < chunkResolution.X; px++)
                        {
                            var vertexIndex = py * chunkResolution.X + px;
                            
                            var index = pixel + py * terrainResolution.X + px;

                            var y = heightmap[index] / 255f;

                            var vertex = chunk.Positions[vertexIndex];
                            vertex.Y = y;
                            chunk.Positions[vertexIndex] = vertex;
                        }
                    }

                    chunk.CalculateFaceNormals();
                    var vao = chunk.ToVirtualVAO($"HeightMap[{x},{z}]");
                    var material = new DeferredSplatTerrainMaterial(terrainShader)
                    {
                        SpecularPower = terrainInfo.SpecularPower,
                        BaseTexture = baseTexture,
                        SplatTexture = splatTexture,
                        BaseTextureTileAmount = terrainInfo.BaseMaterialTileAmount / numChunks.X,
                        TileAmounts = tileAmounts,
                        BlendLayer0Texture0 = textures[0],
                        BlendLayer0Texture1 = textures[1],
                        BlendLayer0Texture2 = textures[2],
                        BlendLayer0Texture3 = textures[3],
                    };
                    var patch = new Model(vao, material);
                    patch.Transform.Position = new Vector3(chunkSize.X * x, 0, chunkSize.Z * z) - halfTerrainSize;
                    patch.Transform.Scale = chunkSize;
                    Terrain.Add(patch);
                }
            }
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
            var instances = new List<HPLEntity>();
            Console.WriteLine();
            Console.Write("Loading Entites");
            foreach (var section in entityDict.sections) {
                var files = new HPLEntity[section.Files.Length];
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
                    var rot = OpenTK.Mathematics.Quaternion.FromEulerAngles(-instance.Rotation.ParseVector3());
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
                            
                            foreach (var renderable in newInstance.Models)
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

            Entities.AddRange(instances);
        }

        public void loadStaticObjects(string staticObjectsPath, AssimpContext assimp, DeferredRenderingGeoMaterial material) {
            var staticObjects = Deserialize<StaticObjects>(staticObjectsPath);

            int attempted = 0, success = 0;
            var failed = new List<string>();
            var instances = new List<HPLEntity>();
            Console.WriteLine();
            Console.Write("Loading Static Objects");
            foreach (var section in staticObjects.sections) {
                var files = new HPLEntity[section.Files.Length];
                foreach (var entFile in section.Files) {
                    try {
                        attempted++;
                        var fullPath = Path.Combine(SOMARoot, entFile.Path);
                        Console.Write(".");

                       //if (fullPath.Contains("05_01_adon_support.DAE") || fullPath.Contains("05_01_adon_box_small.DAE") || fullPath.Contains("phi_tunnel_straight.DAE")) { 
                        if (true) {
                            files[entFile.Id] = new HPLEntity(fullPath, assimp, material);

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
                    var rot = OpenTK.Mathematics.Quaternion.FromEulerAngles(-instance.Rotation.ParseVector3());
                    var scale = instance.Scale.ParseVector3();

                    var model = files[instance.Index];
                    if (model != null) {
                        var newInstance = model.Clone();
                        newInstance.Transform.Position += pos;
                        newInstance.Transform.Rotation += rot;
                        newInstance.Transform.Scale *= scale;

                        var illumColor = instance.IlluminationColor?.ParseVector3() ?? Vector3.One;
                        var albedoTint = instance.ColourMultiplier?.ParseVector3() ?? Vector3.One;
                        foreach (var renderable in newInstance.Models)
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

            Entities.AddRange(instances);
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

                        HPLEntity.getTextures(
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

                        var model = new Model(vao, materialInstance);
                        var ent = new HPLEntity(new List<Model> { model }, vao.BoundingBox);
                        ent.Transform.Position = plane.Position.ParseVector3();
                        //model.Rot += new OpenTK.Mathematics.Quaternion(plane.Rotation.ParseVector3().Negated());

                        Entities.Add(ent);
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
            var instances = new List<HPLEntity>();
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
                            var model = new HPLEntity(fullPath, assimp, material);
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

            Entities.AddRange(instances);
        }

        public Scene ToScene()
        {
            var scene = new Scene()
            {
                PointLights = PointLights,
                SpotLights = SpotLights,
                Terrain = Terrain
            };

            scene.Models.AddRange(Models);

            foreach (var ent in Entities)
            {
                foreach (var model in ent.Models)
                {
                    model.Transform = ent.Transform;
                    model.IsOccluder = ent.IsOccluder;
                    model.IsStatic = ent.IsStatic;
                    scene.Models.Add(model);
                }
            }

            var allAreas = Areas.sections.SelectMany(s => s.Areas).ToList();
            var rawVisibilityAreas = allAreas.Where(a => a.Type == Areas.Section.Area.AreaType.VisibilityArea).ToList();
            var visibilityAreas = rawVisibilityAreas.Select(area =>
            {
                return new VisibilityArea()
                {
                    BoundingBox = area.GetBoundingBox()
                };
            }).ToList();
            var rawVisibilityPortals = allAreas.Where(a => a.Type == Areas.Section.Area.AreaType.VisibilityPortal).ToList();
            var visibilityPortals = rawVisibilityPortals.Select(area =>
            {
                var variables = area.GetProperties();
                variables.TryGetValue("ConnectedAreas1", out var area1Name);
                variables.TryGetValue("ConnectedAreas2", out var area2Name);
                variables.TryGetValue("ConnectedAreas3", out var area3Name);

                var areaIndicies = new List<int>() {
                    rawVisibilityAreas.FindIndex(area => area.Name == area1Name),
                    rawVisibilityAreas.FindIndex(area => area.Name == area2Name),
                    rawVisibilityAreas.FindIndex(area => area.Name == area3Name)
                }.Where(x => x != -1).ToArray();

                var portal = new VisibilityPortal()
                {
                    BoundingBox = area.GetBoundingBox(),
                    VisibilityAreas = areaIndicies
                };
                return portal;
            }).ToList();

            scene.VisibilityAreas = visibilityAreas;
            scene.VisibilityPortals = visibilityPortals;

            return scene;
        }
    }
}
