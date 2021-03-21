using GLOOP.Rendering;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Diagnostics.Contracts;
using System.Text;

namespace GLOOP
{
    public abstract class Camera
    {
        public Vector3 Position;
        public Vector3 Rotation;

        private float znear = 0.1f;
        private float zfar = 1000;
        private float fov = 90;
        private Vector2i shape = new Vector2i();

        public float ZNear
        {
            get => znear;
            set
            {
                lazyProjectionMatrix.Expire();
                znear = value;
            }
        }
        public float ZFar
        {
            get => zfar;
            set
            {
                lazyProjectionMatrix.Expire();
                zfar = value;
            }
        }
        public float FOV
        {
            get => fov;
            set
            {
                lazyProjectionMatrix.Expire();
                fov = value;
            }
        }

        public int Width
        {
            get => shape.X;
            set
            {
                shape.X = value;
                lazyProjectionMatrix.Expire();
            }
        }

        public int Height
        {
            get => shape.Y;
            set
            {
                shape.Y = value;
                lazyProjectionMatrix.Expire();
            }
        }

        public Matrix4 ViewMatrix => lazyViewMatrix.Value;
        protected Lazy<Matrix4> lazyViewMatrix;

        public Matrix4 ProjectionMatrix => lazyProjectionMatrix.Value;
        protected Lazy<Matrix4> lazyProjectionMatrix;

        public Camera(Vector3 pos, Vector3 rot, float fov)
        {
            lazyViewMatrix = new Lazy<Matrix4>(() => MathFunctions.CreateViewMatrix(Position, Rotation));
            lazyProjectionMatrix = new Lazy<Matrix4>(() => MathFunctions.CreateProjectionMatrix(Width, Height, FOV, ZNear, ZFar));

            Position = pos;
            Rotation = rot;
            FOV = fov;
            // Resize is triggered on first frame

            Window.OnResized += OnWidnowResized;
        }

        private void OnWidnowResized(object sender, Vector2i size)
        {
            Width = size.X;
            Height = size.Y;
        }

        public abstract void Update(KeyboardState keyboardState);

        [Pure]
        public bool IsInsideFrustum(Box3 aabb, Matrix4 modelMatrix)
        {
            var cameraMatrix = ProjectionMatrix * ViewMatrix * modelMatrix;
            //var AABBVerts = new[]
            //{
            //    cameraMatrix * new Vector4(-1,-1,-1, 1),
            //    cameraMatrix * new Vector4( 1,-1,-1, 1),
            //    cameraMatrix * new Vector4(-1, 1,-1, 1),
            //    cameraMatrix * new Vector4( 1, 1,-1, 1),

            //    cameraMatrix * new Vector4(-1,-1, 1, 1),
            //    cameraMatrix * new Vector4( 1,-1, 1, 1),
            //    cameraMatrix * new Vector4(-1, 1, 1, 1),
            //    cameraMatrix * new Vector4( 1, 1, 1, 1),
            //};
            var AABBVerts = new[]
            {
                cameraMatrix * new Vector4(aabb.Min.X, aabb.Min.Y, aabb.Min.Z, 1),
                cameraMatrix * new Vector4(aabb.Max.X, aabb.Min.Y, aabb.Min.Z, 1),
                cameraMatrix * new Vector4(aabb.Min.X, aabb.Max.Y, aabb.Min.Z, 1),
                cameraMatrix * new Vector4(aabb.Max.X, aabb.Max.Y, aabb.Min.Z, 1),

                cameraMatrix * new Vector4(aabb.Min.X, aabb.Min.Y, aabb.Max.Z, 1),
                cameraMatrix * new Vector4(aabb.Max.X, aabb.Min.Y, aabb.Max.Z, 1),
                cameraMatrix * new Vector4(aabb.Min.X, aabb.Max.Y, aabb.Max.Z, 1),
                cameraMatrix * new Vector4(aabb.Max.X, aabb.Max.Y, aabb.Max.Z, 1),
            };
            // Check verts against all view planes
            int c1 = 0,
                c2 = 0,
                c3 = 0,
                c4 = 0,
                c5 = 0,
                c6 = 0;
            foreach (var vert in AABBVerts)
            {
                if (vert.X < -vert.W)
                    c1++;
                if (vert.X > vert.W)
                    c2++;
                if (vert.Y < -vert.W)
                    c3++;
                if (vert.Y > vert.W)
                    c4++;
                if (vert.Z < -vert.W)
                    c5++;
                if (vert.Z > vert.W)
                    c6++;
            }

            var inside = !(c1 == 8 || c2 == 8 || c3 == 8 || c4 == 8 || c5 == 8 || c6 == 8);
            if (!inside)
                Console.WriteLine("filtered");
            return inside;
        }

        [Pure]
        public static Vector4 transform(Matrix4 left, float x, float y, float z)
        {
            var dest = new Vector4
            {
                // Note: Removed " * W" from end as W was always 1 and this is performant code
                X = left.M11 * x + left.M21 * y + left.M31 * z + left.M41,
                Y = left.M12 * x + left.M22 * y + left.M32 * z + left.M42,
                Z = left.M13 * x + left.M23 * y + left.M23 * z + left.M43,
                W = left.M14 * x + left.M24 * y + left.M34 * z + left.M44
            };
            return dest;
        }

        [Pure]
        public bool IsInsideFrustum(ref Vector4[] frustumPlanes, Box3 boundingBox, Transform modelTransform)
        {
            var position = boundingBox.Center + modelTransform.Position;
            var size = boundingBox.Size * modelTransform.Scale;
            var radius = Math.Max(Math.Max(size.X, size.Y), size.Z) / 2f;
            return IsInsideFrustum(ref frustumPlanes, position, radius);
        }

        [Pure]
        public static bool IsInsideFrustum(ref Vector4[] frustumPlanes, Vector3 position, float radius)
        {
            for (int i = 0; i < 6; i++)
            {
                var planeEquation = frustumPlanes[i];
                if (planeEquation.X * position.X + planeEquation.Y * position.Y + planeEquation.Z * position.Z + planeEquation.W <= -radius)
                    return false;
            }

            return true;
        }

        [Pure]
        public Vector4[] GetFrustumPlanes()
        {
            Matrix4 pvMatrix = new Matrix4();
            MatrixExtensions.Multiply(ProjectionMatrix, ViewMatrix, ref pvMatrix);
            var frustumPlanes = new[]
            {
                new Vector4(pvMatrix.M14 + pvMatrix.M11, pvMatrix.M24 + pvMatrix.M21, pvMatrix.M34 + pvMatrix.M31, pvMatrix.M44 + pvMatrix.M41).Normalized(),
                new Vector4(pvMatrix.M14 - pvMatrix.M11, pvMatrix.M24 - pvMatrix.M21, pvMatrix.M34 - pvMatrix.M31, pvMatrix.M44 - pvMatrix.M41).Normalized(),
                new Vector4(pvMatrix.M14 + pvMatrix.M12, pvMatrix.M24 + pvMatrix.M22, pvMatrix.M34 + pvMatrix.M32, pvMatrix.M44 + pvMatrix.M42).Normalized(),
                new Vector4(pvMatrix.M14 - pvMatrix.M12, pvMatrix.M24 - pvMatrix.M22, pvMatrix.M34 - pvMatrix.M32, pvMatrix.M44 - pvMatrix.M42).Normalized(),
                new Vector4(pvMatrix.M14 + pvMatrix.M13, pvMatrix.M24 + pvMatrix.M23, pvMatrix.M34 + pvMatrix.M33, pvMatrix.M44 + pvMatrix.M43).Normalized(),
                new Vector4(pvMatrix.M14 - pvMatrix.M13, pvMatrix.M24 - pvMatrix.M23, pvMatrix.M34 - pvMatrix.M33, pvMatrix.M44 - pvMatrix.M43).Normalized(),
            };
            return frustumPlanes;
        }
    }
}
