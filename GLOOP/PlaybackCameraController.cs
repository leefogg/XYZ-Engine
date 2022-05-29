using GLOOP.Util;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GLOOP
{
    public class PlaybackCameraController : ICameraController
    {
        private const int SmoothedFrames = 60 * 2;

        private readonly Vector3[] Positions;
        private readonly Vector3[] Rotations;
        private readonly int OriginalLength;

        public Action OnRecordingComplete;

        public PlaybackCameraController(string recordingPath)
        {
            var csv = File.ReadAllLines(recordingPath);
            OriginalLength = csv.Length;

            var positions = new List<Vector3>();
            var rotations = new List<Vector3>();

            var lineIdx = 0;
            var i = 0;
            foreach (var line in csv)
            {
                if ((lineIdx++) % SmoothedFrames != 0)
                    continue;

                var values = line.Split(',');
                var pos = new Vector3();
                var rot = new Vector3();
                pos.X = float.Parse(values[0]);
                pos.Y = float.Parse(values[1]);
                pos.Z = float.Parse(values[2]);

                rot.X = float.Parse(values[3]);
                rot.Y = float.Parse(values[4]);

                positions.Add(pos);
                rotations.Add(rot);

                i++;
            }

            Positions = positions.ToArray();
            Rotations = rotations.ToArray();
        }

        public void Update(Camera cam, KeyboardState keyboardState)
        {
            var frameIdx = (int)Window.FrameNumber;
            frameIdx %= OriginalLength;

            var offset = frameIdx % SmoothedFrames;
            var lastKeyframe = frameIdx - offset;
            var nextKeyframe = frameIdx + (SmoothedFrames - offset);
            nextKeyframe = Math.Min(OriginalLength-1, nextKeyframe);
            var percent = (float)MathFunctions.Map(frameIdx, lastKeyframe, nextKeyframe, 0, 1);
            lastKeyframe /= SmoothedFrames;
            nextKeyframe /= SmoothedFrames;

            cam.Position = MathFunctions.Tween(Positions[lastKeyframe], Positions[nextKeyframe], percent);
            cam.Rotation = MathFunctions.Tween(Rotations[lastKeyframe], Rotations[nextKeyframe], percent);
            cam.MarkViewDirty();

            if (OnRecordingComplete != null && nextKeyframe == Positions.Length-1)
                OnRecordingComplete.Invoke();
        }
    }
}
