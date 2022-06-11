using GLOOP.Rendering.Debugging;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GLOOP.Rendering
{
    public class VisibilityPortal
    {
        private static readonly Vector4 BoundingBoxColor = new Vector4(1, 0, 0, 0.25f);

        public readonly string Name;
        public readonly Box3 BoundingBox;
        public string[] VisibilityAreas;
        private bool isVisible;
        private byte VisibilityConfidence;
        public bool IsVisible
        {
            get => isVisible;
            set
            {
                var dir = value ? 1 : -1;
                if ((dir == -1 && VisibilityConfidence > 0) || (dir == 1 && VisibilityConfidence < 3))
                    VisibilityConfidence += (byte)dir;
                if (isVisible && VisibilityConfidence == 0)
                    isVisible = false;
                else if (!isVisible && VisibilityConfidence == 3)
                    isVisible = true;
            }
        }

        public Matrix4 ModelMatrix => Matrix4.CreateScale(BoundingBox.Size) * Matrix4.CreateTranslation(BoundingBox.Center);

        public VisibilityPortal(string name, Box3 boundingBox, params string[] visibilityAreaNames)
        {
            Name = name;
            BoundingBox = boundingBox;
            VisibilityAreas = visibilityAreaNames.Where(name => !string.IsNullOrEmpty(name)).ToArray();
        }

        public void RenderBoundingBox()
        {
            Draw.Box(ModelMatrix, BoundingBoxColor);
        }

        public override string ToString() => Name;
    }
}
