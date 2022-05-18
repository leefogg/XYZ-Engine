using GLOOP.Extensions;
using GLOOP.Rendering;
using GLOOP.Rendering.Materials;
using OpenTK;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace GLOOP.HPL.Loading
{
    public class Map {
        private const string SOMARoot = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA";
        public List<HPLEntity> Entities = new List<HPLEntity>();
        public List<Model> Terrain = new List<Model>();
        public List<Rendering.PointLight> PointLights = new List<Rendering.PointLight>();
        public List<Rendering.SpotLight> SpotLights = new List<Rendering.SpotLight>();
        public Areas Areas;

        public Map(string mapPath, Assimp.AssimpContext assimp, DeferredRenderingGeoMaterial material) {
            loadAreas(mapPath + "_Area");
            loadLights(mapPath + "_Light");

            loadStaticObjects(mapPath + "_StaticObject", assimp, material);
            loadEntities(mapPath + "_Entity", assimp, material);
            loadDetailMeshes(mapPath + "_DetailMeshes", assimp, material);
            loadPrimitives(mapPath + "_Primitive", material);
            loadTerrain(mapPath);
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
            var pixelData = IO.DDSImage.GetPixelData(heightmapPath, out var imageWidth, out var imageHeight);

            var terrainResolution = new Vector2i(imageWidth, imageHeight);
            var chunkResolution = new Vector2i(64); // TODO: Bigger the better but limited by ushort index buffer max size
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

            float getHeightMapHeight(int x, int y, int offset = 0)
            {
                var heightmapIndex = offset + y * terrainResolution.X + x;
                return pixelData[heightmapIndex * 3 + 2] / 255f;
            }

            VAOManager.VAOContainer vaoContainer = null;
            var chunk = Rendering.Primitives.CreatePlane(chunkResolution.X - 1, chunkResolution.Y - 1);
            chunk.Move(new Vector3(0.5f, 0f, 0.5f)); // Center origin in the middle
            var defaultUVs = chunk.UVs.ToArray();
            for (int z = 0; z < numChunks.Y; z++)
            {
                for (int x = 0; x < numChunks.X; x++)
                {
                    var chunkPosition = new Vector2i(x, z);
                    chunk.UVs = new List<Vector2>(chunk.UVs.Count);
                    chunk.UVs.AddRange(defaultUVs.Select(uv => (uv * chunkUVScale + chunkUVScale * chunkPosition.Yx).Yx));

                    var startPixelX = chunkPosition.X * chunkResolution.X;
                    var startPixelY = chunkPosition.Y * chunkResolution.Y;
                    startPixelX -= chunkPosition.X;
                    startPixelY -= chunkPosition.Y;

                    var topLeftPixelIndex = startPixelY * terrainResolution.X + startPixelX;
                    for (int py = 0; py < chunkResolution.Y; py++)
                    {
                        for (int px = 0; px < chunkResolution.X; px++)
                        {
                            var vertexIndex = py * chunkResolution.X + px;
                            var heightmapIndex = topLeftPixelIndex + py * terrainResolution.X + px;

                            var y = getHeightMapHeight(px, py, topLeftPixelIndex);

                            var vertex = chunk.Positions[vertexIndex];
                            vertex.Y = y;
                            chunk.Positions[vertexIndex] = vertex;
                        }
                    }

                    chunk.CalculateFaceNormals();

                    // Put all terrain in own VAO
                    vaoContainer ??= VAOManager.Create(
                        new VAO.VAOShape(true, true, true, false),
                        chunk.Indicies.Count * totalChunks,
                        chunk.Positions.Count * totalChunks
                    );
                    var vao = chunk.ToVirtualVAO($"HeightMap[{x},{z}]", vaoContainer);

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

        public void loadEntities(string entitiesFilePath, Assimp.AssimpContext assimp, DeferredRenderingGeoMaterial material) {
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
                            //if (!File.Exists(daePath))
                            //    daePath = Path.Combine(SOMARoot, Path.ChangeExtension(entFile.Path, "dae"));
                            //if (!File.Exists(daePath))
                            //    throw new FileNotFoundException(daePath);
                            //entity.Model.Mesh.FileName = daePath;
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
                    var rot = Quaternion.FromEulerAngles(-instance.Rotation.ParseVector3());
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

        public void loadStaticObjects(string staticObjectsPath, Assimp.AssimpContext assimp, DeferredRenderingGeoMaterial material) {
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
                foreach (var prim in section.Planes) {
                    try {
                        attempted++;
                        var mat = Deserialize<Material>(Path.Combine(SOMARoot, prim.MaterialPath));

                        var endCorner = prim.Scale.ParseVector3();
                        var scale = new Vector2(endCorner.X, endCorner.Z);
                        var uvs = new Vector2[] {
                            prim.Corner1UV.ParseVector2(),
                            prim.Corner2UV.ParseVector2(),
                            prim.Corner3UV.ParseVector2(),
                            prim.Corner4UV.ParseVector2(),
                        };

                        var geo = Rendering.Primitives.CreatePlane(scale, uvs);
                        //var rot = prim.Rotation.ParseVector3().Negated();
                        //geo.Rotate(-MathHelper.RadiansToDegrees(rot.X), -MathHelper.RadiansToDegrees(rot.Y), -MathHelper.RadiansToDegrees(rot.Z));
                        var vao = geo.ToVirtualVAO(prim.Name);

                        HPLEntity.getTextures(
                            mat.Textures?.Diffuse?.Path,
                            mat.Textures?.NormalMap?.Path,
                            mat.Textures?.Specular?.Path,
                            null,
                            string.Empty,
                            out var diffuseTex,
                            out var normalTex,
                            out var specularTex,
                            out var illumTex
                        );

                        var materialInstance = (DeferredRenderingGeoMaterial)material.Clone();
                        materialInstance.TextureRepeat = prim.TileAmount.ParseVector2();
                        materialInstance.TextureOffset = prim.TileOffset.ParseVector2();
                        materialInstance.HasWorldpaceUVs = prim.AlignToWorldCoords;
                        materialInstance.DiffuseTexture = diffuseTex;
                        materialInstance.NormalTexture = normalTex;
                        materialInstance.SpecularTexture = specularTex;
                        materialInstance.IlluminationTexture = illumTex;

                        var model = new Model(vao, materialInstance);
                        var ent = new HPLEntity(new List<Model> { model }, vao.BoundingBox);
                        ent.Transform.Position = prim.Position.ParseVector3();
                        ent.Transform.Rotation = new Quaternion(prim.Rotation.ParseVector3().Negated());

                        Console.Write(".");
                        Entities.Add(ent);
                        success++;
                    }
                    catch (Exception ex) {
                        Console.WriteLine("x");
                    }
                }
            }

            Console.WriteLine($"Loaded {success} out of attempted {attempted}.");
        }

        public void loadDetailMeshes(string detailMeshesPath, Assimp.AssimpContext assimp, DeferredRenderingGeoMaterial material)
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
                        Console.Write(".");
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
                Terrain = Terrain
            };

            // Propgate properties 
            // TODO: Consider using computed properties
            foreach (var ent in Entities)
            {
                foreach (var model in ent.Models)
                {
                    //model.Transform = ent.Transform;
                    model.Transform.Position += ent.Transform.Position;
                    model.Transform.Rotation = ent.Transform.Rotation;
                    model.Transform.Scale *= ent.Transform.Scale;
                    //var multipliedTransform = Matrix4.CreateFromQuaternion(ent.Transform.Rotation) * Matrix4.CreateScale(ent.Transform.Scale) * Matrix4.CreateTranslation(ent.Transform.Position);
                    //model.Transform.Position = multipliedTransform.ExtractTranslation();
                    //model.Transform.Rotation = multipliedTransform.ExtractRotation();
                    //model.Transform.Scale = multipliedTransform.ExtractScale();


                    model.IsOccluder = ent.IsOccluder;
                    model.IsStatic = ent.IsStatic;
                    scene.Models.Add(model);
                }
            }

            // Visibility areas + portals
            var allAreas = Areas.sections.SelectMany(s => s.Areas).ToList();
            
            var rawVisibilityPortals = allAreas.Where(a => a.Type == Areas.Section.Area.AreaType.VisibilityPortal);
            scene.VisibilityPortals = rawVisibilityPortals.Select(area =>
            {
                var variables = area.GetProperties();
                variables.TryGetValue("ConnectedAreas1", out var area1Name);
                variables.TryGetValue("ConnectedAreas2", out var area2Name);
                variables.TryGetValue("ConnectedAreas3", out var area3Name);

                //var areaIndicies = new List<int>() {
                //    rawVisibilityAreas.FindIndex(area => area.Name == area1Name),
                //    rawVisibilityAreas.FindIndex(area => area.Name == area2Name),
                //    rawVisibilityAreas.FindIndex(area => area.Name == area3Name)
                //}.Where(x => x != -1).ToArray();

                var portal = new VisibilityPortal(area.Name, area.GetBoundingBox(), area1Name, area2Name, area3Name);
                return portal;
            }).ToList();

            var rawVisibilityAreas = allAreas.Where(a => a.Type == Areas.Section.Area.AreaType.VisibilityArea).ToList();
            var visibilityAreas = rawVisibilityAreas.Select(area => new VisibilityArea(
                area.Name,
                area.GetBoundingBox(),
                scene.VisibilityPortals.Where(portal => portal.VisibilityAreas.Contains(area.Name))
            )).ToList();
            foreach (var area in visibilityAreas)
                scene.VisibilityAreas[area.Name] = area;
#if DEBUG
            // Ensure that all mentioned areas in portals exist
            foreach (var portal in scene.VisibilityPortals)
            {
                var areas = portal.VisibilityAreas.ToList();
                foreach (var areaName in portal.VisibilityAreas)
                {
                    if (!scene.VisibilityAreas.ContainsKey(areaName))
                    {
                        Console.Error.WriteLine($"Portal '{portal.Name}' refers to area(s) {areaName} that doesn't exist.");
                        areas.Remove(areaName);
                    }
                }
                portal.VisibilityAreas = areas.ToArray();
            }

            // Check if theres any dangling areas
            {
                var allReferencedAreas = scene.VisibilityPortals.SelectMany(portal => portal.VisibilityAreas).Distinct();
                var allAreaNames = scene.VisibilityAreas.Values.Select(area => area.Name);
                var danglingAreas = allAreaNames.Except(allReferencedAreas).ToList();
                if (danglingAreas.Any())
                    Console.Error.WriteLine($"Dangling area '{danglingAreas.ToHumanList()}'");
            }

            //Check all areas of portal are touching the portal
            foreach (var portal in scene.VisibilityPortals)
                foreach (var areaName in portal.VisibilityAreas)
                    if (!scene.VisibilityAreas[areaName].BoundingBox.Contains(portal.BoundingBox))
                        Console.Error.WriteLine($"Area '{areaName}' does not touch its portal '{portal.Name}'");
#endif
            // Add things to accociated area
            var models = Entities.SelectMany(ent => ent.Models).ToList();
            foreach (var area in visibilityAreas) 
            {
                Box3 matToAABB(Matrix4 boundingBoxMatrix)
                {
                    return new Box3(-0.5f, -0.5f, -0.5f, 0.5f, 0.5f, 0.5f).Transform(boundingBoxMatrix);
                }
                Box3 RadToAABB(float radius)
                {
                    return new Box3(-radius, -radius, -radius, radius, radius, radius);
                }

                // "An object will be part of all visibility areas that it is fully inside of."
                var modelsContained = models.Where(model => area.BoundingBox.CompletelyContains(matToAABB(model.BoundingBoxMatrix))).ToList();
                area.Models.AddRange(modelsContained);
                //models.RemoveRange(modelsContained);

                var pointLightsContained = PointLights.Where(light => area.BoundingBox.Contains(light.Position)).ToList();
                area.PointLights.AddRange(pointLightsContained);
                PointLights.RemoveRange(pointLightsContained);

                var spotLightsContained = SpotLights.Where(light => area.BoundingBox.Contains(light.Position)).ToList();
                area.SpotLights.AddRange(spotLightsContained);
                SpotLights.RemoveRange(spotLightsContained);
            }

            // Add things that arent in areas globally
            // "Objects that doesn't fit inside any of the placed areas will get added to a main render container that has an infinite size."
            scene.Models = models.Except(visibilityAreas.SelectMany(area => area.Models)).ToList();
            scene.SpotLights = SpotLights.Except(visibilityAreas.SelectMany(area => area.SpotLights)).ToList();
            scene.PointLights = PointLights.Except(visibilityAreas.SelectMany(area => area.PointLights)).ToList();

            return scene;
        }
    }
}
