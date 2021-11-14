using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class VisibilityPortal
    {
        public readonly string Name;
        public readonly Box3 BoundingBox;
        public readonly string[] VisibilityAreas;

        public VisibilityPortal(string name, Box3 boundingBox, params string[] visibilityAreas)
        {
            Name = name;
            BoundingBox = boundingBox;
            VisibilityAreas = visibilityAreas;
        }
    }
}
