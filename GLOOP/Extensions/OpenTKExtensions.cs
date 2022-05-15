using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GLOOP.Extensions
{
    public static class OpenTKExtensions
    {
        public static int GetSizeInBytes(this PixelInternalFormat format)
        {
            return format switch
            {
                PixelInternalFormat.R8 =>  1,
                PixelInternalFormat.Rg8 =>  2,
                PixelInternalFormat.Rgb => 3,
                PixelInternalFormat.Rgb8 => 3,
                PixelInternalFormat.Srgb8 => 3,
                PixelInternalFormat.Rgb16 => 2 * 3,
                PixelInternalFormat.Rgb16f =>  2 * 3,
                PixelInternalFormat.Rgba => 4,
                PixelInternalFormat.Rgba8 => 4,
                PixelInternalFormat.Srgb8Alpha8 => 4,
                PixelInternalFormat.Rgba16 => 2 * 4,
                PixelInternalFormat.Rgba16f => 2 * 4,
                PixelInternalFormat.Rgb32i => 3 * 4,
                PixelInternalFormat.CompressedRg => 1,
                PixelInternalFormat.CompressedRgba => 1,
                PixelInternalFormat.CompressedRgbaS3tcDxt1Ext => 1, 
                PixelInternalFormat.CompressedRgbaS3tcDxt3Ext => 1,
                PixelInternalFormat.CompressedRgbaS3tcDxt5Ext => 1,
                PixelInternalFormat.CompressedSrgbAlphaS3tcDxt1Ext => 1,
                PixelInternalFormat.CompressedSrgbAlphaS3tcDxt3Ext => 1,
                PixelInternalFormat.CompressedSrgbAlphaS3tcDxt5Ext => 1,
                (PixelInternalFormat)OpenTK.Graphics.OpenGL.All.CompressedLuminanceAlphaLatc2Ext => 1,
                _ => 4
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
                PixelInternalFormat.Srgb => SizedInternalFormat.Rgba8,
                PixelInternalFormat.Srgb8 => SizedInternalFormat.Rgba8,
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

        public static BufferRangeTarget ToRangedTarget(this BufferTarget self)
        {
            return self switch
            {
                BufferTarget.UniformBuffer => BufferRangeTarget.UniformBuffer,
                BufferTarget.ShaderStorageBuffer => BufferRangeTarget.ShaderStorageBuffer,
                BufferTarget.TransformFeedbackBuffer => BufferRangeTarget.TransformFeedbackBuffer,
                BufferTarget.AtomicCounterBuffer => BufferRangeTarget.AtomicCounterBuffer,
                _ => throw new NotSupportedException("No equv buffer range target")
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
            destination.AddRange(self.GetVertcies());
        }

        public static IEnumerable<Vector3> GetVertcies(this Box3 self)
        {
            var min = self.Min;
            var max = self.Max;
            yield return new Vector3(min.X, min.Y, min.Z); // Bottom near left
            yield return new Vector3(min.X, min.Y, max.Z); // Bottom far left
            yield return new Vector3(min.X, max.Y, min.Z); // Top near left
            yield return new Vector3(min.X, max.Y, max.Z); // Top far left
            yield return new Vector3(max.X, min.Y, min.Z); // Bottom near right
            yield return new Vector3(max.X, min.Y, max.Z); // Bottom far right
            yield return new Vector3(max.X, max.Y, min.Z); // Top near right
            yield return new Vector3(max.X, max.Y, max.Z); // Top far right
        }

        public static Box3 ToBoundingBox(this IList<Vector3> self)
        {
            var boundingBox = new Box3(self[0], self[0]);
            foreach (var pos in self.Skip(1))
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

        public static bool CompletelyContains(this Box3 self, Box3 other)
        {
            return self.Min.X <= other.Min.X
                && self.Min.Y <= other.Min.Y
                && self.Min.Z <= other.Min.Z
                && self.Max.X >= other.Max.X
                && self.Max.Y >= other.Max.Y
                && self.Max.Z >= other.Max.Z;
        }

        public static Vector3 Abs(this Vector3 self)
        {
            var x = Math.Abs(self.X);
            var y = Math.Abs(self.Y);
            var z = Math.Abs(self.Z);
            return new Vector3(x, y, z);
        }

        public static SphereBounds ToSphereBounds(this Box3 self)
        {
            return new SphereBounds(self.Center, self.HalfSize.Length);
        }

        public static Box3 Transform(this Box3 self, Rendering.Transform transform)
            => Transform(self, transform.Position, transform.Scale);
        public static Box3 Transform(this Box3 self, Vector3 position, Vector3 scale)
        {
            position += self.Center * scale;
            scale *= self.Size;
            return new Box3(position, position + scale);
        }
    }
}
