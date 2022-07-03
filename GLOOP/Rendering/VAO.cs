using GLOOP.Extensions;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PrimitiveType = OpenTK.Graphics.OpenGL4.PrimitiveType;

namespace GLOOP.Rendering
{
    public class VAO : IDrawable, IDisposable
    {
        public class VAOShape
        {
            public readonly bool IsIndexed;
            public readonly bool HasUVs;
            public readonly bool HasNormals;
            public readonly bool HasTangets;
            public readonly bool HasBones;

            public int NumElements
            {
                get
                {
                    var numElements = 3;
                    if (HasUVs)
                        numElements += 2;
                    if (HasNormals)
                        numElements += 3;
                    if (HasTangets)
                        numElements += 3;
                    if (HasBones)
                        numElements += 4 + 4;

                    return numElements;
                }
            }
            public byte AsBits 
            { 
                get
                {
                    byte bits = 0b00000011;
                    if (HasUVs)
                        bits |= 1 << 2;
                    if (HasNormals)
                        bits |= 1 << 3;
                    if (HasTangets)
                        bits |= 1 << 4;
                    if (HasBones)
                        bits |= 1 << 5;

                    return bits;
                }
            }

            public VAOShape(bool isIndexed, bool hasUVs, bool hasNormals, bool hasTangets, bool hasBones)
            {
                IsIndexed = isIndexed;
                HasUVs = hasUVs;
                HasNormals = hasNormals;
                HasTangets = hasTangets;
                HasBones = hasBones;
            }
            public VAOShape(Geometry geometry)
            {
                IsIndexed = geometry.IsIndexed;
                HasUVs = geometry.HasUVs;
                HasNormals = geometry.HasNormals;
                HasTangets = geometry.HasTangents;
                HasBones = geometry.HasBones;
            }
        }

        private static int CurrentlyBoundHandle;

        public int Handle { get; set; }
        public int NumIndicies { get; set; }

        private readonly VAOShape Shape;
        private readonly int VBO, EBO;

        public VAO(int handle, int numIndicies)
        {
            Handle = handle;
            NumIndicies = numIndicies;
        }

        public VAO(VAOShape shape, string vboName, string eboName)
        {
            Shape = shape;

            Handle = GL.GenVertexArray();

            VBO = GL.GenBuffer();
            EBO = GL.GenBuffer();

            GL.BindVertexArray(Handle);

            vboName = vboName.TrimLabelLength();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.ObjectLabel(ObjectLabelIdentifier.Buffer, VBO, vboName.Length, vboName);

            eboName = eboName.TrimLabelLength();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.ObjectLabel(ObjectLabelIdentifier.Buffer, EBO, eboName.Length, eboName);

            // Calculate size
            var numElements = shape.NumElements;

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(float) * numElements, 0);
            GL.EnableVertexAttribArray(0);
            var offset = 3;

            if (shape.HasUVs)
            {
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, sizeof(float) * numElements, offset * sizeof(float));
                GL.EnableVertexAttribArray(1);
                offset += 2;
            }

            if (shape.HasNormals)
            {
                GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, sizeof(float) * numElements, offset * sizeof(float));
                GL.EnableVertexAttribArray(2);
                offset += 3;
            }

            if (shape.HasTangets)
            {
                GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, sizeof(float) * numElements, offset * sizeof(float));
                GL.EnableVertexAttribArray(3);
            }

            ResourceManager.Add(this);
        }

        public VAO(
            Geometry geometry,
            string vboName, string eboName)
            : this(new VAOShape(geometry), vboName, eboName)
        {
            NumIndicies = geometry.Indicies.Count();

            Allocate(NumIndicies * sizeof(ushort), Shape.NumElements * sizeof(float) * geometry.Positions.Count());
            var indiciesOffset = 0;
            var vertciesOffset = 0;
            FillSubData(
                indiciesOffset,
                vertciesOffset,
                geometry.Indicies, 
                geometry.Positions,
                geometry.UVs, 
                geometry.Normals,
                geometry.Tangents,
                geometry.BoneIds,
                geometry.BoneWeights
            );
        }

        public (int, int, int) FillSubData(
            int firstIndex,
            int firstVertex,
            IEnumerable<uint> vertexIndicies, 
            IEnumerable<Vector3> vertexPositions,
            IEnumerable<Vector2> vertexUVs,
            IEnumerable<Vector3> vertexNormals, 
            IEnumerable<Vector3> vertexTangents,
            IEnumerable<Vector3> vertexBoneIds,
            IEnumerable<Vector3> vertexBoneWeights)
        {
            // Creae EBO
            var indicies = vertexIndicies.Select(x => (ushort)x).ToArray();
            var indiciesSize = indicies.SizeInBytes();
            GL.NamedBufferSubData(EBO, (IntPtr)firstIndex, indiciesSize, indicies);

            // Create and fill VBO
            var positions = vertexPositions.GetFloats().ToArray();
            var uvs = vertexUVs?.GetFloats().ToArray();
            var normals = vertexNormals?.GetFloats().ToArray() ?? new float[0];
            var tangents = vertexTangents?.GetFloats().ToArray() ?? new float[0];
            var boneIds = vertexBoneIds?.GetFloats().ToArray() ?? new float[0];
            var boneWeights = vertexBoneWeights?.GetFloats().ToArray() ?? new float[0];
            var verts = createVertexArray(positions, uvs, normals, tangents, boneIds, boneWeights);
            var vertciesSize = verts.SizeInBytes();
            GL.NamedBufferSubData(VBO, (IntPtr)firstVertex, vertciesSize, verts);

            return (indicies.Count(), indiciesSize, vertciesSize);
        }

        public void Allocate(int sizeOfIndicies, int sizeOfVertcies)
        {
            GL.NamedBufferData(EBO, sizeOfIndicies, (IntPtr)0, BufferUsageHint.StaticDraw);
            Metrics.ModelsIndiciesBytesUsed += (ulong)sizeOfIndicies;

            GL.NamedBufferData(VBO, sizeOfVertcies, (IntPtr)0, BufferUsageHint.StaticDraw);
            Metrics.ModelsVertciesBytesUsed += (ulong)sizeOfVertcies;
        }

        private Type getSmallestDataFormat(uint count)
        {
            if (count > ushort.MaxValue)
                return typeof(uint);
            if (count > byte.MaxValue)
                return typeof(ushort);
            return typeof(byte);
        }

        private float[] createVertexArray(float[] positions, float[] uvs, float[] normals, float[] tangents, float[] boneIds, float[] boneWeights)
        {
            var posIndex = 0;
            var uvIndex = 0;
            var normalIndex = 0;
            var tangentIndex = 0;
            var boneIdIndex = 0;
            var boneWeightIndex = 0;

            bool hasUVs = uvs?.Any() ?? false;
            bool hasNormals = normals?.Any() ?? false;
            bool hasTangents = tangents?.Any() ?? false;
            bool hasBones = (boneIds?.Any() ?? false) && (boneWeights?.Any() ?? false);

            int stride = 3;
            if (hasUVs)
                stride += 2;
            if (hasNormals)
                stride += 3;
            if (hasTangents)
                stride += 3;
            if (hasBones)
                stride += 4 + 4;
            var estimatedNumFloats = stride * (positions.Length / 3);
            var verts = new List<float>(estimatedNumFloats);

            while (posIndex < positions.Length)
            {
                verts.Add(positions[posIndex++]);
                verts.Add(positions[posIndex++]);
                verts.Add(positions[posIndex++]);

                if (hasUVs)
                {
                    verts.Add(uvs[uvIndex++]);
                    verts.Add(uvs[uvIndex++]);
                }

                if (hasNormals)
                {
                    verts.Add(normals[normalIndex++]);
                    verts.Add(normals[normalIndex++]);
                    verts.Add(normals[normalIndex++]);
                }

                if (hasTangents)
                {
                    verts.Add(tangents[tangentIndex++]);
                    verts.Add(tangents[tangentIndex++]);
                    verts.Add(tangents[tangentIndex++]);
                }

                if (hasBones)
                {
                    verts.Add(boneIds[boneIdIndex++]);
                    verts.Add(boneIds[boneIdIndex++]);
                    verts.Add(boneIds[boneIdIndex++]);
                    verts.Add(boneIds[boneIdIndex++]);

                    verts.Add(boneWeights[boneWeightIndex++]);
                    verts.Add(boneWeights[boneWeightIndex++]);
                    verts.Add(boneWeights[boneWeightIndex++]);
                    verts.Add(boneWeights[boneWeightIndex++]);
                }
            }

            Debug.Assert(verts.Count == estimatedNumFloats, "Incorrect estimation");
            Debug.Assert(posIndex == positions.Length, "Spare vertcies not uploaded");

            return verts.ToArray();
        }

        public void Draw(PrimitiveType renderMode = PrimitiveType.Triangles) => Draw(renderMode, 1);
        public void Draw(PrimitiveType renderMode = PrimitiveType.Triangles, int numInstances = 1)
        {
            Bind();

            if (numInstances > 1)
                GL.DrawElementsInstanced(renderMode, NumIndicies, DrawElementsType.UnsignedShort, (IntPtr)0, numInstances);
            else
                GL.DrawElements(renderMode, NumIndicies, DrawElementsType.UnsignedShort, (IntPtr)0);

            //GL.DrawElementsBaseVertex(
            //    renderMode,
            //    (int)NumIndicies,
            //    DrawElementsType.UnsignedInt,
            //    (IntPtr)0,
            //    0
            //);
            //GL.DrawElementsInstancedBaseVertexBaseInstance(
            //    renderMode,
            //    NumIndicies,
            //    DrawElementsType.UnsignedInt,
            //    (IntPtr)0,
            //    1,
            //    0,
            //    0
            //);
            //var data = new DrawElementsIndirectData(
            //    NumIndicies,
            //    1,
            //    0,
            //    0,
            //    0
            //);
            //var draws = new[] { data };

            //var DIB = GL.GenBuffer();
            //GL.BindBuffer(BufferTarget.DrawIndirectBuffer, DIB);
            //GL.BufferData(BufferTarget.DrawIndirectBuffer, sizeof(uint) * 5 * draws.Length, draws, BufferUsageHint.StaticDraw);

            //GL.DrawElementsIndirect(renderMode, DrawElementsType.UnsignedInt, (IntPtr)0);
            //GL.MultiDrawElementsIndirect(renderMode, DrawElementsType.UnsignedInt, (IntPtr)0, 1, sizeof(uint) * 5);
        }

        public void Bind()
        {
            Bind(Handle);
        }

        public static void Bind(int Handle)
        {
            if (CurrentlyBoundHandle != Handle)
            {
                GL.BindVertexArray(Handle);
                CurrentlyBoundHandle = Handle;
            }
        }

        public void Dispose()
        {
            GL.DeleteBuffer(Handle);
        }

    }
}
