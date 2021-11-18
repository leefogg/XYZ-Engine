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
