using GLOOP.Extensions;
using GLOOP.Rendering;
using OpenTK;
using OpenTK.Graphics.ES30;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static GLOOP.Extensions.Vector3Extensions;

namespace GLOOP
{
    public class Geometry
    {
        public List<Vector3> Positions;
        public List<Vector2> UVs;
        public List<Vector3> Normals;
        public List<Vector3> Tangents;
        public List<uint> Indicies;

        public void NormalizeScale()
        {
            var bb = GetBoundingBox();
            var scale = Math.Max(bb.Size.X, Math.Max(bb.Size.Y, bb.Size.Z));
            Scale(new Vector3(1f / scale));
        }

        public void Centre() {
            var bb = GetBoundingBox();
            Move(bb.Center);
        }

        public void Scale(Vector3 scale) {
            if (float.IsInfinity(scale.Z))
                scale.Z = 1;
            for (var i = 0; i < Positions.Count; i++) {
                var pos = Positions[i];
                Multiply(ref pos, scale);
                Positions[i] = pos;
            }
        }

        public void Move(Vector3 offset) {
            for (var i=0; i<Positions.Count; i++) {
                var pos = Positions[i];
                pos.X += offset.X;
                pos.Y += offset.Y;
                pos.Z += offset.Z;
                Positions[i] = pos;
            }
        }

        public Box3 GetBoundingBox() => Positions.ToBoundingBox();

        public void CalculateTangents()
        {
            Tangents = new List<Vector3>();
            for (var v=0; v < Indicies.Count;) {
                var a = (int)Indicies[v++];
                var b = (int)Indicies[v++];
                var c = (int)Indicies[v++];
                Tangents.Add(calculateVertexTangent(a, b, c));
                Tangents.Add(calculateVertexTangent(b, c, a));
                Tangents.Add(calculateVertexTangent(c, a, b));
            }
        }

        private Vector3 calculateVertexTangent(int a, int b, int c)
        {
            var pos1 = Positions[a];
            var pos2 = Positions[b];
            var pos3 = Positions[c];
            var uv1 = UVs[a];
            var uv2 = UVs[b];
            var uv3 = UVs[c];

            var edge1 = pos2 - pos1;
            var edge2 = pos3 - pos1;
            var deltaUV1 = uv2 - uv1;
            var deltaUV2 = uv3 - uv1;

            var f = 1f / (deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y);
            Vector3 tangent;
            tangent.X = f * (deltaUV2.Y * edge1.X - deltaUV1.Y * edge2.X);
            tangent.Y = f * (deltaUV2.Y * edge1.Y - deltaUV1.Y * edge2.Y);
            tangent.Z = f * (deltaUV2.Y * edge1.Z - deltaUV1.Y * edge2.Z);
            return tangent;
        }

        public void CalculateFaceNormals()
        {
            Normals.Clear();
            for (var v = 0; v < Indicies.Count;)
            {
                var pos1 = Positions[(int)Indicies[v++]];
                var pos2 = Positions[(int)Indicies[v++]];
                var pos3 = Positions[(int)Indicies[v++]];
                //var normal = getFaceNormal(pos1, pos2, pos3);
                //Normals.Add(normal);
                //Normals.Add(normal);
                //Normals.Add(normal);
            }
        }

        private Vector3 getFaceNormal(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            var u = v2 - v1;
            var v = v3 - v1;
            var normal = new Vector3(
                u.Y * v.Z - u.Z * v.Y,
                u.Z * v.X - u.X * v.Z,
                u.X * v.Y - u.Y * v.X
            );
            normal.Normalize();

            return normal;
        }

        internal VAO ToVAO(string vaoName)
        {
            var vboName = vaoName + "VBO";
            if (vboName.Length > Globals.MaxLabelLength)
                vboName = vboName[^Globals.MaxLabelLength..];
            var eboName = vaoName + "EBO";
            if (eboName.Length > Globals.MaxLabelLength)
                eboName = eboName[^Globals.MaxLabelLength..];
            //CalculateFaceNormals();

            return new VAO(Indicies, Positions, UVs, Normals, Tangents, vboName, vaoName);
        }
        internal VirtualVAO ToVirtualVAO(string vaoName)
        {
            var vboName = vaoName + "VBO";
            if (vboName.Length > Globals.MaxLabelLength)
                vboName = vboName[^Globals.MaxLabelLength..];
            var eboName = vaoName + "EBO";
            if (eboName.Length > Globals.MaxLabelLength)
                eboName = eboName[^Globals.MaxLabelLength..];
            //CalculateFaceNormals();

            var vao = VAOManager.Get(
                new VAO.VAOShape(true, UVs?.Any() ?? false, Normals?.Any() ?? false, Tangents?.Any() ?? false),
                Indicies,
                Positions,
                UVs,
                Normals,
                Tangents
            );
            vao.BoundingBox = GetBoundingBox();
            return vao;
        }
    }
}
