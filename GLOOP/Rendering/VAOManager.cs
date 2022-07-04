using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using static GLOOP.Rendering.VAO;

namespace GLOOP.Rendering
{
    public class VAOManager
    {
        public class VAOContainer
        {
            public VAO vao;
            public int ReservedIndiciesBytes;
            public int ReservedVertciesBytes;
            public int UsedIndiciesBytes;
            public int UsedVertciesBytes;
#if DEBUG
            public float IndiciesPercentageFilled => (UsedIndiciesBytes / ReservedIndiciesBytes) * 100f;
            public float VertciesPercentageFilled => (ReservedVertciesBytes/ ReservedVertciesBytes) * 100f;
#endif
        }
        private static int Total;

        private static Dictionary<byte, List<VAOContainer>> pool = new Dictionary<byte, List<VAOContainer>>();

        private static VAOContainer GetOrCreateContainer(
            VAOShape shape, 
            int requiredIndiciesBytes, 
            int requiredVertciesBytes)
        {
            if (pool.ContainsKey(shape.AsBits))
            {
                foreach (var alloc in pool[shape.AsBits])
                {
                    var indexBytesFree = alloc.ReservedIndiciesBytes - alloc.UsedIndiciesBytes;
                    var vertexBytesFree = alloc.ReservedVertciesBytes - alloc.UsedVertciesBytes;
                    if (indexBytesFree >= requiredIndiciesBytes && vertexBytesFree >= requiredVertciesBytes)
                        return alloc;
                }
            }

            return create(
                shape,
                Math.Max(6 * sizeof(ushort) * 1024 * 1024, requiredIndiciesBytes),
                Math.Max(45 * sizeof(float) * 1024 * 1024, requiredVertciesBytes)
            );
        }
        public static VAOContainer Create(VAOShape shape, int numIndicies, int numVertcies)
        {
            var numIndiciesBytes = numIndicies * sizeof(ushort);
            var numVertciesBytes = numVertcies * sizeof(float) * shape.NumElements;
            return create(shape, numIndiciesBytes, numVertciesBytes);
        }
        private static VAOContainer create(VAOShape shape, int numIndiciesBytes, int numVertciesBytes)
        {
            var vao = new VAO(shape, "PooledVAO" + Total, "PooledVAOEBO" + Total);
            Total++;

            vao.Allocate(numIndiciesBytes, numVertciesBytes);
            var alloc = new VAOContainer
            {
                ReservedIndiciesBytes = numIndiciesBytes,
                ReservedVertciesBytes = numVertciesBytes,
                UsedIndiciesBytes = 0,
                UsedVertciesBytes = 0,
                vao = vao
            };

            var shapeBits = shape.AsBits;
            if (pool.ContainsKey(shapeBits))
                pool[shapeBits].Add(alloc);
            else
                pool.Add(shapeBits, new List<VAOContainer>() { alloc });

            return alloc;
        }

        public static VirtualVAO Get(
            VAOShape shape,
            IEnumerable<uint> vertexIndicies,
            IEnumerable<Vector3> vertexPositions,
            IEnumerable<Vector2> vertexUVs,
            IEnumerable<Vector3> vertexNormals,
            IEnumerable<Vector3> vertexTangents,
            IEnumerable<Vector4> vertexBoneIds,
            IEnumerable<Vector4> vertexBoneWeights,
            VAOContainer containerOverride = null)
        {
            var numIndicies = vertexIndicies.Count();
            Debug.Assert(numIndicies < ushort.MaxValue, "Model with more than UI16 indicies");
            var estimatedNumIndicies = vertexIndicies.Count() * sizeof(ushort);
            var estimatedNumVertcies = shape.NumElements * sizeof(float) * vertexPositions.Count();
            var container = containerOverride ?? GetOrCreateContainer(
                shape, 
                estimatedNumIndicies,
                estimatedNumVertcies
            );
            Debug.Assert(container.UsedIndiciesBytes + estimatedNumIndicies <= container.ReservedIndiciesBytes, "VAO IBO overflow.");
            Debug.Assert(container.UsedVertciesBytes + estimatedNumVertcies <= container.ReservedVertciesBytes, "VAO EBO overflow.");

            var numIndiciesBefore = container.UsedIndiciesBytes;
            var numVertciesBefore = container.UsedVertciesBytes;
            (int indiciesCount, int usedIndiciesBytes, int usedVertciesBytes) = container.vao.FillSubData(
                container.UsedIndiciesBytes,
                container.UsedVertciesBytes,
                vertexIndicies,
                vertexPositions,
                vertexUVs,
                vertexNormals,
                vertexTangents,
                vertexBoneIds,
                vertexBoneWeights
            );
            Debug.Assert(estimatedNumIndicies == usedIndiciesBytes);
            Debug.Assert(estimatedNumVertcies == usedVertciesBytes);

            var vao = new VirtualVAO(
                new DrawElementsIndirectData(
                    (uint)indiciesCount,
                    (uint)numIndiciesBefore,
                    (uint)numVertciesBefore / ((uint)shape.NumElements * sizeof(float)),
                    1,
                    0
                ),
                container.vao
            );


            container.UsedIndiciesBytes += usedIndiciesBytes;
            container.UsedVertciesBytes += usedVertciesBytes;
            if (container.UsedIndiciesBytes - numIndiciesBefore != estimatedNumIndicies || container.UsedVertciesBytes - numVertciesBefore != estimatedNumVertcies)
                Console.WriteLine("Incorrect estimate");

            return vao;
        }
    }
}
