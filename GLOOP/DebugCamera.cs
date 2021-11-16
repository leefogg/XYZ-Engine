using OpenTK;
using OpenTK.Input;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Text;
using static GLOOP.Extensions.Vector3Extensions;

namespace GLOOP {
    public class DebugCamera : Camera {
        public static float WalkingSpeed = 0.01f;
        public static float MouseSpeed = 0.2f;
        public static int MAX_LOOK_UP = 85;
        public static int MAX_LOOK_DOWN = -85;
        private Vector3 velocity = new Vector3();
        private bool invertY = true;

        public DebugCamera(Vector3 pos, Vector3 rot, float fov) : base(pos, rot, fov)
        {
        }

        public override void Update(KeyboardState keyboardState) {
            var timescaler = 1f;

            if (Mouse.Grabbed) {
                var mousedirection = Mouse.CurrentState.Delta;
                mousedirection.Y *= invertY ? -1 : 1;

                if (mousedirection.LengthFast > 1)
                    lazyViewMatrix.Expire();

                Multiply(ref mousedirection, MouseSpeed);

                if (Rotation.Y + mousedirection.X >= 360) {
                    Rotation.Y = Rotation.Y + mousedirection.X - 360;
                } else if (Rotation.Y + mousedirection.X < 0) {
                    Rotation.Y = 360 - Rotation.Y + mousedirection.X;
                } else {
                    Rotation.Y += mousedirection.X;
                }
                if (Rotation.X - mousedirection.Y >= MAX_LOOK_DOWN && Rotation.X - mousedirection.Y <= MAX_LOOK_UP) {
                    Rotation.X -= mousedirection.Y;
                } else if (Rotation.X - mousedirection.Y < MAX_LOOK_DOWN) {
                    Rotation.X = MAX_LOOK_DOWN;
                } else if (Rotation.X - mousedirection.Y > MAX_LOOK_UP) {
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
                if (movefaster && !moveslower) {
                    walkingspeed *= 10f;
                }
                if (moveslower && !movefaster) {
                    walkingspeed /= 10f;
                }

                var additionalvelcity = new Vector3();
                if (forward && right && !left && !backward) {
                    float angle = Rotation.Y + 45;
                    float oblique = walkingspeed * timescaler;
                    float adjacent = oblique * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                    float opposite = (float)(Math.Sin(MathHelper.DegreesToRadians(angle)) * oblique);
                    additionalvelcity.Z -= adjacent;
                    additionalvelcity.X += opposite;
                }
                if (forward && left && !right && !backward) {
                    float angle = Rotation.Y - 45;
                    float oblique = walkingspeed * timescaler;
                    float adjacent = oblique * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                    float opposite = (float)(Math.Sin(MathHelper.DegreesToRadians(angle)) * oblique);
                    additionalvelcity.Z -= adjacent;
                    additionalvelcity.X += opposite;
                }
                if (forward && !left && !right && !backward) {
                    float angle = Rotation.Y;
                    float oblique = walkingspeed * timescaler;
                    float adjacent = oblique * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                    float opposite = (float)(Math.Sin(MathHelper.DegreesToRadians(angle)) * oblique);
                    additionalvelcity.X += opposite;
                    additionalvelcity.Z -= adjacent;
                }
                if (backward && left && !right && !forward) {
                    float angle = Rotation.Y - 135;
                    float oblique = walkingspeed * timescaler;
                    float adjacent = oblique * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                    float opposite = (float)(Math.Sin(MathHelper.DegreesToRadians(angle)) * oblique);
                    additionalvelcity.Z -= adjacent;
                    additionalvelcity.X += opposite;
                }
                if (backward && right && !left && !forward) {
                    float angle = Rotation.Y + 135;
                    float oblique = walkingspeed * timescaler;
                    float adjacent = oblique * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                    float opposite = (float)(Math.Sin(MathHelper.DegreesToRadians(angle)) * oblique);
                    additionalvelcity.Z -= adjacent;
                    additionalvelcity.X += opposite;
                }
                if (backward && !forward && !left && !right) {
                    float angle = Rotation.Y;
                    float oblique = -walkingspeed * timescaler;
                    float adjacent = oblique * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                    float opposite = (float)(Math.Sin(MathHelper.DegreesToRadians(angle)) * oblique);
                    additionalvelcity.Z -= adjacent;
                    additionalvelcity.X += opposite;
                }
                if (left && !right && !forward && !backward) {
                    float angle = Rotation.Y - 90;
                    float oblique = walkingspeed * timescaler;
                    float adjacent = oblique * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                    float opposite = (float)(Math.Sin(MathHelper.DegreesToRadians(angle)) * oblique);
                    additionalvelcity.Z -= adjacent;
                    additionalvelcity.X += opposite;
                }
                if (right && !left && !forward && !backward) {
                    float angle = Rotation.Y + 90;
                    float oblique = walkingspeed * timescaler;
                    float adjacent = oblique * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                    float opposite = (float)(Math.Sin(MathHelper.DegreesToRadians(angle)) * oblique);
                    additionalvelcity.Z -= adjacent;
                    additionalvelcity.X += opposite;
                }

                if (up && !down) {
                    var newPositionY = walkingspeed * timescaler;
                    additionalvelcity.Y += newPositionY;
                }
                if (down && !up) {
                    var newPositionY = walkingspeed * timescaler;
                    additionalvelcity.Y -= newPositionY;
                }

                velocity.X += additionalvelcity.X;
                velocity.Y += additionalvelcity.Y;
                velocity.Z += additionalvelcity.Z;
            }

            if (velocity.LengthFast > 0.001)
            lazyViewMatrix.Expire();

            Position.X += velocity.X;
            Position.Y += velocity.Y;
            Position.Z += velocity.Z;
            Multiply(ref velocity, 0.8f);


            if (keyboardState.IsKeyDown(Keys.P)) {
                Console.WriteLine(Position);
            }
        }
    }
}
