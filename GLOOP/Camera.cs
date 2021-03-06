using OpenTK;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
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
    }
}
