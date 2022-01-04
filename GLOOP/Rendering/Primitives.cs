using Assimp;
using GLOOP.Extensions;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GLOOP.Rendering
{
    public static class Primitives
    {
        private static VAO _quad;
        public static VAO Quad => _quad ??= CreateQuad();

        private static VAO _sphere;
        public static VAO Sphere => _sphere ??= loadSimpleModel("assets/models/icosphere.obj", "Internal_Sphere");

        private static VAO _cube;
        public static VAO Cube => _cube ??= loadSimpleModel("assets/models/cube.obj", "Internal_Cube");

        private static VAO _wireframeCube;
        public static VAO WireframeCube
        {
            get
            {
                if (_wireframeCube == null)
                {
                    var half = Vector3.One / 2f;
                    // https://stackoverflow.com/questions/25195363/draw-cube-vertices-with-fewest-number-of-steps
                    _wireframeCube = new Geometry
                    {
                        Positions = new Box3(-half, half).GetVertcies().ToList(),
                        Indicies = new List<uint>()
                        {
                            0,4,4,6,6,7,7,3,3,2,2,0,0,1,1,5,5,4,4,6,6,2,2,3,3,1,1,5,5,7
                        }
                    }.ToVAO("_internal_wireframeCube");
                }

                return _wireframeCube;
            }
        }

        private static VAO _frustum;
        public static VAO Frustum
        {
            get
            {
                if (_frustum == null)
                {
                    _frustum = new Geometry()
                    {
                        Positions = new List<Vector3>()
                        {
                            new Vector3( 0,  0, 0),
                            new Vector3( 1,  1, 1), // Top Right    // 1
                            new Vector3( 1, -1, 1), // Bottom right // 2
                            new Vector3(-1, -1, 1), // Bottom left  // 3
                            new Vector3(-1,  1, 1), // Top Left     // 4
                        },
                        Indicies = new List<uint>()
                        {
                            0,1,4, // Top
                            0,2,1, // Right
                            0,3,2, // Bottom
                            0,4,3, // Left
                            2,4,1, // Front 1
                            2,3,4, // Front 2
                        }
                    }.ToVAO("_internal_frustum");
                }

                return _frustum;
            }
        }

        private static VAO loadSimpleModel(string path, string VAOName)
        {
            var assimp = new AssimpContext();
            var scene = assimp.ImportFile(path, PostProcessSteps.GenerateNormals | PostProcessSteps.CalculateTangentSpace);
            var mesh = scene.Meshes[0];

            var geo = new Geometry
            {
                Positions = mesh.Vertices.Select(v => new Vector3(v.X, v.Y, v.Z)).ToList(),
                UVs = mesh.TextureCoordinateChannels[0].Select(uv => new Vector2(uv.X, uv.Y)).ToList(),
                Indicies = mesh.GetIndices().Cast<uint>().ToList(),
                Normals = mesh.Normals.Select(n => new Vector3(n.X, n.Y, n.Z)).ToList(),
                Tangents = mesh.Tangents.Select(n => new Vector3(n.X, n.Y, n.Z)).ToList()
            };
            var bb = geo.GetBoundingBox();
            geo.NormalizeScale();
            bb = geo.GetBoundingBox();

            return geo.ToVAO(VAOName);
        }

        public static Geometry CreatePlane(Vector2 TLCorner, Vector2[] uvs)
        {
            var geo = new Geometry();
            geo.Positions = new List<Vector3>(){
                new Vector3(1, 0, 1), // top right
                new Vector3(1, 0, 0), // bottom right
                new Vector3(0, 0, 0), // bottom left
                new Vector3(0, 0, 1)  // top left
            };
            geo.UVs = new List<Vector2>(uvs);
            geo.Normals = new List<Vector3>
            {
                new Vector3(0,1,0),
                new Vector3(0,1,0),
                new Vector3(0,1,0),
                new Vector3(0,1,0),
            };
            geo.Tangents = new List<Vector3>
            {
                new Vector3(0,0,1),
                new Vector3(0,0,1),
                new Vector3(0,0,1),
                new Vector3(0,0,1),
            };
            geo.Indicies = new List<uint>() {
                0, 1, 3,
                1, 2, 3
            };
            geo.Scale(new Vector3(TLCorner.X, 1, TLCorner.Y));

            return geo;
        }

        // https://www.patrykgalach.com/2019/07/29/procedural-terrain-pt1-plane/?cn-reloaded=1
        public static Geometry CreatePlane(int xSections, int zSections)
        {
            var numVerts = (xSections + 1) * (zSections + 1);
            var numIndicies = xSections * zSections * 2 * 3;
            var geo = new Geometry
            {
                Positions = new List<Vector3>(numVerts),
                UVs = new List<Vector2>(numVerts),
                Indicies = new List<uint>(numIndicies)
            };

            var stepSize = new Vector2(1f / xSections, 1f / zSections);
            for (var z = 0; z <= zSections; z++)
            {
                for (var x = 0; x <= xSections; x++)
                {
                    geo.Positions.Add(new Vector3(stepSize.X * x, 0, stepSize.Y * z));
                    geo.UVs.Add(new Vector2(stepSize.X * z, stepSize.Y * x));
                }
            }

            for (var i = 0; i < numIndicies; i++)
                geo.Indicies.Add(0);

            for (var z = 0; z < zSections; z++)
            {
                for (var x = 0; x < xSections; x++)
                {
                    var i = (z * xSections + x) * 6;

                    geo.Indicies[i + 0] = (uint)(z * (xSections + 1) + x);
                    geo.Indicies[i + 1] = (uint)((z + 1) * (xSections + 1) + x);
                    geo.Indicies[i + 2] = (uint)((z + 1) * (xSections + 1) + x + 1);
                    geo.Indicies[i + 3] = (uint)(z * (xSections + 1) + x);
                    geo.Indicies[i + 4] = (uint)((z + 1) * (xSections + 1) + x + 1);
                    geo.Indicies[i + 5] = (uint)(z * (xSections + 1) + x + 1);
                }
            }

            geo.CalculateFaceNormals();
            return geo;
        }

        public static VAO CreateQuad()
        {
            var geo = new Geometry()
            {
                Indicies = new List<uint>
                {
                    3, 1, 0,
                    3, 2, 1
                },
                Positions = new List<Vector3>
                {
                    new Vector3( 1, 1, 0),
                    new Vector3( 1,-1, 0),
                    new Vector3(-1,-1, 0),
                    new Vector3(-1, 1, 0),
                },
                UVs = new List<Vector2>
                {
                    new Vector2(1, 1),
                    new Vector2(1, 0),
                    new Vector2(0, 0),
                    new Vector2(0, 1),
                }
            };

            return geo.ToVAO("Internal_Quad");

        }
    }
}
