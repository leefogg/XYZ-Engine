using GLOOP.Rendering;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static GLOOP.Rendering.VAO;

namespace GLOOP
{
    public class VAOManager
    {
        private class VAOAllocation
        {
            public VAO vao;
            public int ReservedIndiciesBytes;
            public int ReservedVertciesBytes;
            public int UsedIndiciesBytes;
            public int UsedVertciesBytes;
        }
        private static int Total;

        private static Dictionary<byte, List<VAOAllocation>> pool = new Dictionary<byte, List<VAOAllocation>>();

        private static VAOAllocation GetOrCreateAllocation(
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

            return Create(
                shape,
                Math.Max(3 * sizeof(uint) * 1024 * 1024, requiredIndiciesBytes),
                Math.Max(23 * sizeof(float) * 1024 * 1024, requiredVertciesBytes)
            );
        }
        private static VAOAllocation Create(VAOShape shape, int numIndiciesBytes, int numVertciesBytes)
        {
            var vao = new VAO(shape, "PooledVAO" + Total, "PooledVAOEBO" + Total);
            Total++;

            vao.Allocate(numIndiciesBytes, numVertciesBytes);
            var alloc = new VAOAllocation
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
                pool.Add(shapeBits, new List<VAOAllocation>() { alloc });

            return alloc;
        }

        public static VirtualVAO Get(
            VAOShape shape,
            IEnumerable<int> vertexIndicies,
            IEnumerable<Vector3> vertexPositions,
            IEnumerable<Vector2> vertexUVs,
            IEnumerable<Vector3> vertexNormals,
            IEnumerable<Vector3> vertexTangents)
        {
            var estimatedNumIndicies = vertexIndicies.Count() * sizeof(uint);
            var estimatedNumVertcies = shape.NumElements * sizeof(float) * vertexPositions.Count();
            var alloc = GetOrCreateAllocation(
                shape, 
                estimatedNumIndicies,
                estimatedNumVertcies
            );
            var numIndiciesBefore = alloc.UsedIndiciesBytes;
            var numVertciesBefore = alloc.UsedVertciesBytes;
            (int indiciesCount, int usedIndiciesBytes, int usedVertciesBytes) = alloc.vao.FillSubData(
                alloc.UsedIndiciesBytes,
                alloc.UsedVertciesBytes,
                vertexIndicies,
                vertexPositions,
                vertexUVs,
                vertexNormals,
                vertexTangents
            );

            var vao = new VirtualVAO(
                new DrawElementsIndirectData(
                    (uint)indiciesCount,
                    (uint)numIndiciesBefore,
                    (uint)numVertciesBefore / ((uint)shape.NumElements * sizeof(float)),
                    1,
                    0
                ),
                alloc.vao
            );

            alloc.UsedIndiciesBytes += usedIndiciesBytes;
            alloc.UsedVertciesBytes += usedVertciesBytes;
            if (alloc.UsedIndiciesBytes - numIndiciesBefore != estimatedNumIndicies || alloc.UsedVertciesBytes - numVertciesBefore != estimatedNumVertcies)
                Console.WriteLine("Incorrect estimate");

            return vao;
        }
    }
}
