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

        public int VAOHandle { get; set; }
        public int NumIndicies { get; set; }
        public int NumVertcies { get; set; }

        private readonly VAOShape Shape;
        private readonly int VBOHandle, EBOHandle;

        public VAO(int handle, int numIndicies, int numVertcies)
        {
            VAOHandle = handle;
            NumIndicies = numIndicies;
            NumVertcies = numVertcies;
        }

        public VAO(VAOShape shape, string vboName, string eboName)
        {
            Shape = shape;
            VAOHandle = GL.GenVertexArray();

            GL.BindVertexArray(VAOHandle);

            VBOHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOHandle);
            vboName = vboName.TrimLabelLength();
            GL.ObjectLabel(ObjectLabelIdentifier.Buffer, VBOHandle, vboName.Length, vboName);

            if (shape.IsIndexed)
            {
                EBOHandle = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBOHandle);
                eboName = eboName.TrimLabelLength();
                GL.ObjectLabel(ObjectLabelIdentifier.Buffer, EBOHandle, eboName.Length, eboName);
            }

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
                offset += 3;
            }

            if (shape.HasBones)
            {
                // Bone IDs
                GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, sizeof(float) * numElements, offset * sizeof(float));
                GL.EnableVertexAttribArray(4);
                offset += 4;
                // Bone Weights
                GL.VertexAttribPointer(5, 4, VertexAttribPointerType.Float, false, sizeof(float) * numElements, offset * sizeof(float));
                GL.EnableVertexAttribArray(5);
            }

            ResourceManager.Add(this);
        }

        public VAO(
            Geometry geometry,
            string vboName, string eboName)
            : this(new VAOShape(geometry), vboName, eboName)
        {
            NumIndicies = geometry.Indicies?.Count() ?? 0;
            NumVertcies = geometry.Positions.Count;

            Allocate(NumIndicies * sizeof(ushort), Shape.NumElements * sizeof(float) * geometry.Positions.Count());
            FillSubData(
                0,
                0,
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
            IEnumerable<Vector4> vertexBoneIds,
            IEnumerable<Vector4> vertexBoneWeights)
        {
            var numIndicies = vertexIndicies?.Count() ?? 0;
            var indiciesSize = numIndicies * sizeof(ushort);
            if (numIndicies > 0)
            {
                Debug.Assert(EBOHandle != 0, "Created VBO with different shape then provided data for.");
                // Create EBO
                var indicies = vertexIndicies.Select(x => (ushort)x).ToArray();
                GL.NamedBufferSubData(EBOHandle, (IntPtr)firstIndex, indiciesSize, indicies);
            }

            // Create and fill VBO
            var positions = vertexPositions.GetFloats().ToArray();
            var uvs = vertexUVs?.GetFloats().ToArray();
            var normals = vertexNormals?.GetFloats().ToArray() ?? new float[0];
            var tangents = vertexTangents?.GetFloats().ToArray() ?? new float[0];
            var boneIds = vertexBoneIds?.GetFloats().ToArray() ?? new float[0];
            var boneWeights = vertexBoneWeights?.GetFloats().ToArray() ?? new float[0];
            var verts = createVertexArray(positions, uvs, normals, tangents, boneIds, boneWeights);
            var vertciesSize = verts.SizeInBytes();
            GL.NamedBufferSubData(VBOHandle, (IntPtr)firstVertex, vertciesSize, verts);

            return (numIndicies, indiciesSize, vertciesSize);
        }

        public void Allocate(int sizeOfIndicies, int sizeOfVertcies)
        {
            GL.NamedBufferData(EBOHandle, sizeOfIndicies, (IntPtr)0, BufferUsageHint.StaticDraw);
            Metrics.ModelsIndiciesBytesUsed += (ulong)sizeOfIndicies;

            GL.NamedBufferData(VBOHandle, sizeOfVertcies, (IntPtr)0, BufferUsageHint.StaticDraw);
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
                if (NumIndicies > 0)
                    GL.DrawElements(renderMode, NumIndicies, DrawElementsType.UnsignedShort, (IntPtr)0);
                else
                    GL.DrawArrays(renderMode, 0, NumVertcies);
        }

        public void Bind()
        {
            Bind(VAOHandle);
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
            GL.DeleteBuffer(VAOHandle);
        }
    }
}
