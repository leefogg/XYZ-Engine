using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Extensions
{
    public static class OpenTKExtensions
    {
        public static int GetSizeInBytes(this PixelInternalFormat format)
        {
            var floatSize = sizeof(float);
            return format switch
            {
                PixelInternalFormat.R8 => floatSize * 1,
                PixelInternalFormat.Rg8 => floatSize * 2,
                PixelInternalFormat.Rgb => floatSize * 3,
                PixelInternalFormat.Rgb8 => floatSize * 3,
                PixelInternalFormat.Rgb16 => floatSize * 2 * 3,
                PixelInternalFormat.Rgb16f => floatSize * 2 * 3,
                PixelInternalFormat.Rgba => floatSize * 4,
                PixelInternalFormat.Rgba8 => floatSize * 4,
                PixelInternalFormat.Rgba16 => floatSize * 2 * 4,
                PixelInternalFormat.Rgba16f => floatSize * 2 * 4,
                PixelInternalFormat.CompressedRg => 1,
                PixelInternalFormat.CompressedRgba => 1,
                PixelInternalFormat.CompressedRgbaS3tcDxt1Ext => 1, 
                PixelInternalFormat.CompressedRgbaS3tcDxt3Ext => 1,
                PixelInternalFormat.CompressedRgbaS3tcDxt5Ext => 1,
                (PixelInternalFormat)OpenTK.Graphics.OpenGL.All.CompressedLuminanceAlphaLatc2Ext => 1,
                _ => floatSize * 4
            };
        }

        public static SizedInternalFormat ToSizedFormat(this PixelInternalFormat format)
        {
            return format switch
            {
                PixelInternalFormat.R8 => SizedInternalFormat.R8,
                PixelInternalFormat.Rg8 => SizedInternalFormat.Rg8,
                PixelInternalFormat.Rgb => (SizedInternalFormat)All.Rgb8, // Missing. Unimplemented.
                PixelInternalFormat.Rgb8 => (SizedInternalFormat)All.Rgb8, // Missing. Unimplemented.
                PixelInternalFormat.Rgb16 => (SizedInternalFormat)All.Rgb16, // Missing. Unimplemented.
                PixelInternalFormat.Rgb16f => (SizedInternalFormat)All.Rgb16f, // Missing. Unimplemented.
                PixelInternalFormat.Rgba => SizedInternalFormat.Rgba8,
                PixelInternalFormat.Rgba8 => SizedInternalFormat.Rgba8,
                PixelInternalFormat.Rgba16 => SizedInternalFormat.Rgba16,
                PixelInternalFormat.Rgba16f => SizedInternalFormat.Rgba16f,
                PixelInternalFormat.CompressedRg => SizedInternalFormat.Rg8,
                PixelInternalFormat.CompressedRgb => (SizedInternalFormat)All.Rgb8, // Missing. Unimplemented.
                PixelInternalFormat.CompressedRgba => SizedInternalFormat.Rgba8,
                _ => SizedInternalFormat.Rgba16f
            };
        }

        public static void AsFloats(this Vector2 self, float[] dest, ref int start)
        {
            dest[start++] = self.X;
            dest[start++] = self.Y;
        }

        public static void AsFloats(this Vector3 self, float[] dest, ref int start)
        {
            dest[start++] = self.X;
            dest[start++] = self.Y;
            dest[start++] = self.Z;
        }

        public static void AsFloats(this Vector4 self, float[] dest, ref int start)
        {
            dest[start++] = self.X;
            dest[start++] = self.Y;
            dest[start++] = self.Z;
            dest[start++] = self.W;
        }

        public static float[] AsFloats(this Vector4[] self)
        {
            var floats = new float[self.Length * 4];

            var index = 0;
            foreach (var f in self)
                f.AsFloats(floats, ref index);

            return floats;
        }

        public static void RotateAround(this List<Vector3> self, Quaternion rotation, Vector3 origin)
        {
            for (var i=0; i<self.Count; i++)
            {
                var temp = new Vector4(self[i], 0);
                temp.Xyz -= origin;
                temp = rotation * temp;
                temp.Xyz += origin;
                self[i] = temp.Xyz;
            }
        }

        public static void GetCorners(this Box3 self, List<Vector3> destination)
        {
            var min = self.Min;
            var max = self.Max;
            destination.Add(new Vector3(min.X, min.Y, min.Z)); // Bottom near left
            destination.Add(new Vector3(min.X, min.Y, max.Z)); // Bottom far left
            destination.Add(new Vector3(min.X, max.Y, min.Z)); // Top near left
            destination.Add(new Vector3(min.X, max.Y, max.Z)); // Top far left
            destination.Add(new Vector3(max.X, min.Y, min.Z)); // Bottom near right
            destination.Add(new Vector3(max.X, min.Y, max.Z)); // Bottom far right
            destination.Add(new Vector3(max.X, max.Y, min.Z)); // Top near right
            destination.Add(new Vector3(max.X, max.Y, max.Z)); // Top far right
        }

        public static Box3 ToBoundingBox(this IEnumerable<Vector3> self)
        {
            var boundingBox = new Box3();
            foreach (var pos in self)
                boundingBox.Inflate(pos);

            return boundingBox;
        }

        public static Box3 Union(this Box3 self, Box3 other)
        {
            var allCorners = new List<Vector3>(16);
            self.GetCorners(allCorners);
            other.GetCorners(allCorners);
            return allCorners.ToBoundingBox();
        }

        public static Box3 Rotated(this Box3 self, Quaternion rotation)
        {
            var allCorners = new List<Vector3>(8);
            self.GetCorners(allCorners);
            allCorners.RotateAround(rotation, self.Center);
            return allCorners.ToBoundingBox();
        }

        public static Box3 Transform(this Box3 self, Matrix4 modelMatrix)
        {
            var allCorners = new List<Vector3>(8);
            self.GetCorners(allCorners);
            for (int i = 0; i < allCorners.Count; i++)
                allCorners[i] = Vector3.TransformPosition(allCorners[i], modelMatrix);
            return allCorners.ToBoundingBox();
        }
    }
}
