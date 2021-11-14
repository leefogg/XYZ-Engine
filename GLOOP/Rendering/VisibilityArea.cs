using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class VisibilityArea : RenderableArea
    {
        public readonly Box3 BoundingBox;

        public VisibilityArea(string name, Box3 boundingBox)
            : base(name)
        {
            BoundingBox = boundingBox;
        }
    }
}
