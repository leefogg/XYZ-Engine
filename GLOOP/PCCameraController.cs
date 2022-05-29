using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace GLOOP
{
    public class PCCameraController : ICameraController
    {
        public const float WalkingSpeed = 0.01f;
        public const float MouseSpeed = 0.2f;
        public const int MAX_LOOK_UP = 85;
        public const int MAX_LOOK_DOWN = -85;

        private Vector3 Velocity = new Vector3();
        private bool InvertY = true;

        public void Update(Camera cam, KeyboardState keyboardState)
        {
            var timescaler = 1f; // TODO: Time scaler
            var Rotation = cam.Rotation;

            if (Mouse.Grabbed)
            {
                var mousedirection = Mouse.CurrentState.Delta;
                mousedirection.Y *= InvertY ? -1 : 1;

                mousedirection *= MouseSpeed;

                if (Rotation.Y + mousedirection.X >= 360)
                {
                    Rotation.Y = Rotation.Y + mousedirection.X - 360;
                }
                else if (Rotation.Y + mousedirection.X < 0)
                {
                    Rotation.Y = 360 - Rotation.Y + mousedirection.X;
                }
                else
                {
                    Rotation.Y += mousedirection.X;
                }
                if (Rotation.X - mousedirection.Y >= MAX_LOOK_DOWN && Rotation.X - mousedirection.Y <= MAX_LOOK_UP)
                {
                    Rotation.X -= mousedirection.Y;
                }
                else if (Rotation.X - mousedirection.Y < MAX_LOOK_DOWN)
                {
                    Rotation.X = MAX_LOOK_DOWN;
                }
                else if (Rotation.X - mousedirection.Y > MAX_LOOK_UP)
                {
                    Rotation.X = MAX_LOOK_UP;
                }

                var forward = keyboardState.IsKeyDown(Keys.W);
                var backward = keyboardState.IsKeyDown(Keys.S);
                var left = keyboardState.IsKeyDown(Keys.A);
                var right = keyboardState.IsKeyDown(Keys.D);
                var up = keyboardState.IsKeyDown(Keys.Space);
                var down = keyboardState.IsKeyDown(Keys.LeftShift);
                var movefaster = keyboardState.IsKeyDown(Keys.LeftControl);
                var moveslower = keyboardState.IsKeyDown(Keys.Tab);

                var walkingspeed = WalkingSpeed;
                if (movefaster && !moveslower)
                {
                    walkingspeed *= 10f;
                }
                if (moveslower && !movefaster)
                {
                    walkingspeed /= 10f;
                }

                var additionalvelcity = new Vector3();
                if (forward && right && !left && !backward)
                {
                    float angle = Rotation.Y + 45;
                    float oblique = walkingspeed * timescaler;
                    float adjacent = oblique * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                    float opposite = (float)(Math.Sin(MathHelper.DegreesToRadians(angle)) * oblique);
                    additionalvelcity.Z -= adjacent;
                    additionalvelcity.X += opposite;
                }
                if (forward && left && !right && !backward)
                {
                    float angle = Rotation.Y - 45;
                    float oblique = walkingspeed * timescaler;
                    float adjacent = oblique * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                    float opposite = (float)(Math.Sin(MathHelper.DegreesToRadians(angle)) * oblique);
                    additionalvelcity.Z -= adjacent;
                    additionalvelcity.X += opposite;
                }
                if (forward && !left && !right && !backward)
                {
                    float angle = Rotation.Y;
                    float oblique = walkingspeed * timescaler;
                    float adjacent = oblique * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                    float opposite = (float)(Math.Sin(MathHelper.DegreesToRadians(angle)) * oblique);
                    additionalvelcity.X += opposite;
                    additionalvelcity.Z -= adjacent;
                }
                if (backward && left && !right && !forward)
                {
                    float angle = Rotation.Y - 135;
                    float oblique = walkingspeed * timescaler;
                    float adjacent = oblique * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                    float opposite = (float)(Math.Sin(MathHelper.DegreesToRadians(angle)) * oblique);
                    additionalvelcity.Z -= adjacent;
                    additionalvelcity.X += opposite;
                }
                if (backward && right && !left && !forward)
                {
                    float angle = Rotation.Y + 135;
                    float oblique = walkingspeed * timescaler;
                    float adjacent = oblique * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                    float opposite = (float)(Math.Sin(MathHelper.DegreesToRadians(angle)) * oblique);
                    additionalvelcity.Z -= adjacent;
                    additionalvelcity.X += opposite;
                }
                if (backward && !forward && !left && !right)
                {
                    float angle = Rotation.Y;
                    float oblique = -walkingspeed * timescaler;
                    float adjacent = oblique * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                    float opposite = (float)(Math.Sin(MathHelper.DegreesToRadians(angle)) * oblique);
                    additionalvelcity.Z -= adjacent;
                    additionalvelcity.X += opposite;
                }
                if (left && !right && !forward && !backward)
                {
                    float angle = Rotation.Y - 90;
                    float oblique = walkingspeed * timescaler;
                    float adjacent = oblique * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                    float opposite = (float)(Math.Sin(MathHelper.DegreesToRadians(angle)) * oblique);
                    additionalvelcity.Z -= adjacent;
                    additionalvelcity.X += opposite;
                }
                if (right && !left && !forward && !backward)
                {
                    float angle = Rotation.Y + 90;
                    float oblique = walkingspeed * timescaler;
                    float adjacent = oblique * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                    float opposite = (float)(Math.Sin(MathHelper.DegreesToRadians(angle)) * oblique);
                    additionalvelcity.Z -= adjacent;
                    additionalvelcity.X += opposite;
                }

                if (up && !down)
                {
                    var newPositionY = walkingspeed * timescaler;
                    additionalvelcity.Y += newPositionY;
                }
                if (down && !up)
                {
                    var newPositionY = walkingspeed * timescaler;
                    additionalvelcity.Y -= newPositionY;
                }

                Velocity += additionalvelcity;
            }
            cam.Rotation = Rotation;

            cam.Position += Velocity * timescaler;
            Velocity *= 0.8f * timescaler;

            //cam.MarkPerspectiveDirty();
            cam.MarkViewDirty();

            if (keyboardState.IsKeyDown(Keys.P))
            {
                Console.WriteLine(cam.Position);
            }
        }
    }
}
