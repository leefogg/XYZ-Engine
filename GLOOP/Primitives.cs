using Assimp;
using GLOOP.Rendering;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GLOOP
{
    public static class Primitives
    {
        private static VAO _quad;
        public static VAO Quad
        {
            get
            {
                if (_quad == null)
                    _quad = CreateQuad();
                return _quad;
            }
        }

        private static VAO _sphere;
        public static VAO Sphere
        {
            get
            {
                if (_sphere == null)
                    _sphere = loadSimpleModel("assets/models/sphere.obj", "Internal_Sphere");

                return _sphere;
            }
        }

        private static VAO _cube;
        public static VAO Cube
        {
            get
            {
                if (_cube == null)
                    _cube = loadSimpleModel("assets/models/cube.obj", "Internal_Cube");

                return _cube;
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
