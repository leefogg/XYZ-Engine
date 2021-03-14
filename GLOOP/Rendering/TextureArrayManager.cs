using GLOOP.Rendering;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace GLOOP.Rendering
{
    public class TextureArrayManager
    {
        private const int DefaultArraySliceCount = 10;

        private class TextureAllocation
        {
            public readonly TextureArray Texture;
            public readonly int ReservedSlices;
            public ushort UsedSlices;

            public TextureAllocation(TextureArray texture, int reservedSlices)
            {
                Texture = texture;
                ReservedSlices = reservedSlices;
            }
        }
        
        public class TextureShapeSummary
        {
            public readonly TextureShape Shape;
            public readonly uint AllocatedSlices, WastedSlices;

            public TextureShapeSummary(TextureShape shape, uint allocatedSlices, uint wastedSlices)
            {
                Shape = shape;
                AllocatedSlices = allocatedSlices;
                WastedSlices = wastedSlices;
            }
        }

        private static Dictionary<TextureShape, List<TextureAllocation>> pool = new Dictionary<TextureShape, List<TextureAllocation>>();

        private static TextureAllocation getOrCreateAllocation(TextureShape shape)
        {
            foreach (var s in pool.Keys)
                if (s == shape) 
                    foreach (var alloc in pool[s])
                        if (alloc.UsedSlices < alloc.ReservedSlices)
                            return alloc;

            return createTexture(shape);
        }
        public static void CreateTexture(TextureShape shape, ushort numLayers)
        {
            var matchingShapes = pool.Count(x => x.Key == shape);
            createTexture(shape, numLayers, $"PooledTextureArray{shape.Width}x{shape.Height}[{matchingShapes}]");
        }
        private static TextureAllocation createTexture(TextureShape shape, ushort numLayers = DefaultArraySliceCount, string name = "")
        {
            List<TextureAllocation> foundPool = null;
            foreach (var s in pool.Keys)
            {
                if (s == shape)
                {
                    foundPool = pool[s];
                    break;
                }
            }

            ushort slicesToReserve = numLayers;
            var texture = new TextureArray(shape, slicesToReserve, name);
            var newAlloc = new TextureAllocation(texture, slicesToReserve);
            if (foundPool != null)
                foundPool.Add(newAlloc);
            else
                pool.Add(shape, new List<TextureAllocation>() { newAlloc });

            return newAlloc;
        }

        public static TextureSlice Get(
            string path,
            PixelInternalFormat format = PixelInternalFormat.Rgba, 
            TextureWrapMode wrapMode = TextureWrapMode.ClampToBorder, 
            TextureMinFilter filterMode = TextureMinFilter.Linear,
            bool hasMipMaps = false
        ) {
            using var image = new Bitmap(path);

            var data = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );
            var shape = new TextureShape(
                (ushort)image.Width, 
                (ushort)image.Height,
                hasMipMaps,
                format, 
                wrapMode,
                filterMode
            );

            var alloc = getOrCreateAllocation(shape);

            return alloc.Texture.WriteSubData(alloc.UsedSlices++, data.Scan0, PixelFormat.Bgra);
        }

        public static TextureShapeSummary[] GetSummary()
        {
            var shapes = pool
                .Keys
                .Select(shape => new TextureShapeSummary(
                    shape, 
                    (uint)pool[shape].Sum(tex => tex.UsedSlices),
                    (uint)pool[shape].Sum(tex => tex.ReservedSlices - tex.UsedSlices
                )))
                .OrderByDescending(alloc => alloc.AllocatedSlices)
                .ToArray();
            return shapes;
        }
    }
}
