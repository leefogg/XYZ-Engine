using GLOOP.Extensions;
using OpenTK;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static GLOOP.Extensions.Vector3Extensions;

namespace GLOOP.Rendering
{
    public class Geometry
    {
        public List<Vector3> Positions;
        public List<Vector2> UVs;
        public List<Vector3> Normals;
        public List<Vector3> Tangents;
        public List<Vector4> BoneIds;
        public List<Vector4> BoneWeights;
        public List<uint> Indicies;

        public VAO.VAOShape Shape => new VAO.VAOShape(this);

        public bool HasUVs => UVs?.Any() ?? false;
        public bool HasNormals => Normals?.Any() ?? false;
        public bool HasTangents => Tangents?.Any() ?? false;
        public bool IsIndexed => Indicies?.Any() ?? false;
        public bool HasBones => (BoneIds?.Any() ?? false) && (BoneWeights?.Any() ?? false);

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

        public void Rotate(Vector3 rotation) => Rotate(rotation.X, rotation.Y, rotation.Z);
        public void Rotate(float xdeg, float ydeg, float zdeg)
        {
            var matrix = Matrix4.CreateFromQuaternion(new Quaternion(MathHelper.DegreesToRadians(xdeg), MathHelper.DegreesToRadians(ydeg), MathHelper.DegreesToRadians(zdeg)));
            for (int i = 0; i < Positions.Count; i++)
                Positions[i] = (matrix * new Vector4(Positions[i], 0)).Xyz;
        }

        public Box3 GetBoundingBox() => Positions.ToBoundingBox();

        public void CalculateTangents()
        {
            Tangents = new List<Vector3>(Indicies.Count * 3);
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

        // https://www.iquilezles.org/www/articles/normals/normals.htm
        public void CalculateFaceNormals()
        {
            if (Normals == null)
                Normals = new List<Vector3>();
            Normals.Resize(Positions.Count);

            for (var i = 0; i < Indicies.Count;)
            {
                var ia = (int)Indicies[i++];
                var ib = (int)Indicies[i++];
                var ic = (int)Indicies[i++];

                var pos1 = Positions[ia];
                var pos2 = Positions[ib];
                var pos3 = Positions[ic];

                var e1 = pos1 - pos2;
                var e2 = pos3 - pos2;
                var no = Vector3.Cross(e2, e1);

                Normals[ia] += no;
                Normals[ib] += no;
                Normals[ic] += no;
            }

            foreach (var normal in Normals)
                normal.Normalize();
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

            return new VAO(this, vboName, vaoName);
        }
        public VirtualVAO ToVirtualVAO(VAOManager.VAOContainer containerOverride = null)
        {
            var vao = VAOManager.Get(
                Shape,
                Indicies,
                Positions,
                UVs,
                Normals,
                Tangents,
                BoneIds,
                BoneWeights,
                containerOverride
            );
            vao.BoundingBox = GetBoundingBox();
            return vao;
        }
    }
}
