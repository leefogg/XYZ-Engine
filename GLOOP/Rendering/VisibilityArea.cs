using GLOOP.Rendering.Debugging;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GLOOP.Rendering
{
    public class VisibilityArea : RenderableArea
    {
        private static readonly Vector4 BoundingBoxColor = new Vector4(1, 0, 0, 1);

        public readonly Box3 BoundingBox;
        public readonly VisibilityPortal[] ConnectingPortals;

        public Matrix4 ModelMatrix => Matrix4.CreateScale(BoundingBox.Size) * Matrix4.CreateTranslation(BoundingBox.Center);

        public VisibilityArea(string name, Box3 boundingBox, IEnumerable<VisibilityPortal> connectingPortals)
            : base(name)
        {
            BoundingBox = boundingBox;
            ConnectingPortals = connectingPortals.ToArray();
        }


        public void RenderBoundingBox()
        {
            Draw.BoundingBox(ModelMatrix, BoundingBoxColor);
        }
    }
}
