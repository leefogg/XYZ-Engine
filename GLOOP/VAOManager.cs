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
            var alloc = GetOrCreateAllocation(
                shape, 
                vertexIndicies.Count() * sizeof(uint),
                shape.NumElements * sizeof(float) * vertexPositions.Count()
            );
            var vao = alloc.vao.FillSubData(
                ref alloc.UsedIndiciesBytes,
                ref alloc.UsedVertciesBytes,
                vertexIndicies,
                vertexPositions,
                vertexUVs,
                vertexNormals,
                vertexTangents
            );
            vao.description.BaseVertex /= (uint)shape.NumElements * sizeof(float);
            return vao;
        }
    }
}
