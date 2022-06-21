using GLOOP.Extensions;
using GLOOP.Rendering;
using GLOOP.Rendering.Debugging;
using GLOOP.Util;
using GLOOP.Util.Structures;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Diagnostics.Contracts;

namespace GLOOP
{
    public class Camera
    {
        public static Camera Current;

        public Vector3 Position;
        public Vector3 Rotation;

        private float znear = 0.1f;
        private float zfar = 1000;
        private float fov = 90;
        private Vector2i shape = new Vector2i();

        public ICameraController CameraController;

        public float ZNear
        {
            get => znear;
            set
            {
                lazyProjectionMatrix = null;
                znear = value;
            }
        }
        public float ZFar
        {
            get => zfar;
            set
            {
                lazyProjectionMatrix = null;
                zfar = value;
            }
        }

        public float FOV
        {
            get => fov;
            set
            {
                lazyProjectionMatrix = null;
                fov = value;
            }
        }

        public int Width
        {
            get => shape.X;
            set
            {
                shape.X = value;
                lazyProjectionMatrix = null;
            }
        }

        public int Height
        {
            get => shape.Y;
            set
            {
                shape.Y = value;
                lazyProjectionMatrix = null;
            }
        }

        public Matrix4 ViewMatrix => lazyViewMatrix ??= MathFunctions.CreateViewMatrix(Position, Rotation);
        protected Matrix4? lazyViewMatrix;

        public Matrix4 ProjectionMatrix => lazyProjectionMatrix ??= MathFunctions.CreateProjectionMatrix(Width, Height, FOV, ZNear, ZFar);
        protected Matrix4? lazyProjectionMatrix;

        public Vector4[] FrustumPlanes => lazyFrustumPlanes ??= GetFrustumPlanes();
        protected Vector4[] lazyFrustumPlanes;

        public Camera(Vector3 pos, Vector3 rot, float fov)
        {
            Position = pos;
            Rotation = rot;
            FOV = fov;

            // Resize is triggered on first frame
            Window.OnResized += OnWidnowResized;
        }

        public void MarkViewDirty()
        {
            lazyViewMatrix = null;
            lazyFrustumPlanes = null;
        }
        public void MarkProjectionDirty()
        {
            lazyProjectionMatrix = null;
            lazyFrustumPlanes = null;
        }

        private void OnWidnowResized(object sender, Vector2i size)
        {
            Width = size.X;
            Height = size.Y;
        }

        public void Update(KeyboardState keyboardState) => CameraController?.Update(this, keyboardState);

        public bool IntersectsFrustum(SphereBounds sphere) => IntersectsFrustum(sphere.Position, sphere.Radius);
        public bool IntersectsFrustum(Vector3 center, float radius)
        {
            // Project sphere into view space.
            var viewSpace = new Vector4(center.X, center.Y, center.Z, 1) * ViewMatrix;
            center = (viewSpace / viewSpace.W).Xyz;  // Perspective division.

            var r0 = ProjectionMatrix.Column0;
            var r1 = ProjectionMatrix.Column1;
            var r2 = ProjectionMatrix.Column2;
            var r3 = ProjectionMatrix.Column3;

            radius = -radius;
            var visible = 
                   radius <= Vector3.Dot(center, (r3 + r0).Xyz)  // Left plane
                && radius <= Vector3.Dot(center, (r3 - r0).Xyz)  // Right plane
                && radius <= Vector3.Dot(center, (r3 + r1).Xyz)  // Bottom plane
                && radius <= Vector3.Dot(center, (r3 - r1).Xyz)  // Top plane
                && radius <= Vector3.Dot(center,  r3.Xyz)        // Near plane
                && radius <= Vector3.Dot(center, (r3 - r2).Xyz); // Far plane
            return visible;
        }

        public bool IsInsideFrustum(in Box3 boundingBox, in Transform modelTransform)
        {
            var position = boundingBox.Center + modelTransform.Position;
            var size = boundingBox.Size * modelTransform.Scale;
            var radius = Math.Max(Math.Max(size.X, size.Y), size.Z) / 2f;
            return IsInsideFrustum(position, radius);
        }

        public bool IsInsideFrustum(Vector3 position, float radius)
        {
            using var profiler = EventProfiler.Profile("Visibility");

            for (int i = 0; i < 6; i++)
            {
                var planeEquation = FrustumPlanes[i];
                if (planeEquation.X * position.X + planeEquation.Y * position.Y + planeEquation.Z * position.Z + planeEquation.W <= -radius)
                    return false;
            }

            return true;
        }

        private Vector4[] GetFrustumPlanes()
        {
            var pvMatrix = new Matrix4();
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

        //https://iquilezles.org/articles/frustumcorrect/
        public bool IntersectsFrustumFast(in Box3 worldspaceAABB)
        {
            using var profiler = EventProfiler.Profile("Visibility");
            // check box outside/inside of frustum
            foreach (var plane in FrustumPlanes)
            {
                //if (worldspaceAABB.GetVertcies().All(v => Vector4.Dot(plane, new Vector4(v.X, v.Y, v.Z, 1)) < 0))
                //    return false;

                // Optimized -
                var passed =
                    Vector4.Dot(plane, new Vector4(worldspaceAABB.Min.X, worldspaceAABB.Min.Y, worldspaceAABB.Min.Z, 1)) < 0
                 && Vector4.Dot(plane, new Vector4(worldspaceAABB.Max.X, worldspaceAABB.Min.Y, worldspaceAABB.Min.Z, 1)) < 0
                 && Vector4.Dot(plane, new Vector4(worldspaceAABB.Min.X, worldspaceAABB.Max.Y, worldspaceAABB.Min.Z, 1)) < 0
                 && Vector4.Dot(plane, new Vector4(worldspaceAABB.Max.X, worldspaceAABB.Max.Y, worldspaceAABB.Min.Z, 1)) < 0
                 && Vector4.Dot(plane, new Vector4(worldspaceAABB.Min.X, worldspaceAABB.Min.Y, worldspaceAABB.Max.Z, 1)) < 0
                 && Vector4.Dot(plane, new Vector4(worldspaceAABB.Max.X, worldspaceAABB.Min.Y, worldspaceAABB.Max.Z, 1)) < 0
                 && Vector4.Dot(plane, new Vector4(worldspaceAABB.Min.X, worldspaceAABB.Max.Y, worldspaceAABB.Max.Z, 1)) < 0
                 && Vector4.Dot(plane, new Vector4(worldspaceAABB.Max.X, worldspaceAABB.Max.Y, worldspaceAABB.Max.Z, 1)) < 0;
                if (passed)
                    return false;
            }

            return true;
        }

        public bool IntersectsFrustum(in Box3 worldspaceAABB)
        {
            if (!IntersectsFrustumFast(worldspaceAABB))
                return false;

            // check frustum outside/inside box
            //int out;
            //out= 0; for (int i = 0; i < 8; i++) out += ((fru.mPoints[i].x > box.mMaxX) ? 1 : 0); if ( out== 8 ) return false;
            //out= 0; for (int i = 0; i < 8; i++) out += ((fru.mPoints[i].x < box.mMinX) ? 1 : 0); if ( out== 8 ) return false;
            //out= 0; for (int i = 0; i < 8; i++) out += ((fru.mPoints[i].y > box.mMaxY) ? 1 : 0); if ( out== 8 ) return false;
            //out= 0; for (int i = 0; i < 8; i++) out += ((fru.mPoints[i].y < box.mMinY) ? 1 : 0); if ( out== 8 ) return false;
            //out= 0; for (int i = 0; i < 8; i++) out += ((fru.mPoints[i].z > box.mMaxZ) ? 1 : 0); if ( out== 8 ) return false;
            //out= 0; for (int i = 0; i < 8; i++) out += ((fru.mPoints[i].z < box.mMinZ) ? 1 : 0); if ( out== 8 ) return false;

            return true;
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

        // Untested
        [Pure]
        public bool ProjectSphere(Vector3 center, float radius, out Box2 aabb)
        {
            if (center.Z < ZNear + radius)
            {
                aabb = default;
                return false;
            }

            var cx = -center.Xz;
            var vx = new Vector2((float)Math.Sqrt(Vector2.Dot(cx, cx) - radius * radius), radius);
            var minx = cx * new Matrix2(vx.X, vx.Y, -vx.Y, vx.X);
            var maxx = cx * new Matrix2(vx.X, -vx.Y, vx.Y, vx.X);

            var cy = center.Yz;
            var vy = new Vector2((float)Math.Sqrt(Vector2.Dot(cy, cy) - radius * radius), radius);
            var miny = cy * new Matrix2(vy.X, vy.Y, -vy.Y, vy.X);
            var maxy = cy * new Matrix2(vy.X, -vy.Y, vy.Y, vy.X);

            var p00 = ProjectionMatrix.M11;
            var p11 = ProjectionMatrix.M22;
            var box = new Vector4(
                minx.X / minx.Y * p00,
                miny.X / miny.Y * p11,
                maxx.X / maxx.Y * p00,
                maxy.X / maxy.Y * p11
            );
            box = box.Xwzy * new Vector4(.5f, -.5f, .5f, .5f) + new Vector4(0.5f);
            aabb = new Box2(box.Xy, box.Zw - box.Xy);
            return true;
        }
    }
}
