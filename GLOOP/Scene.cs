using GLOOP.Extensions;
using GLOOP.Rendering;
using GLOOP.Rendering.Debugging;
using GLOOP.Rendering.Materials;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GLOOP
{
    public class Scene : RenderableArea
    {
        public List<Model> Terrain = new List<Model>();
        public List<VisibilityPortal> VisibilityPortals = new List<VisibilityPortal>();
        public Dictionary<string, VisibilityArea> VisibilityAreas = new Dictionary<string, VisibilityArea>();

        public Scene() : base("World")
        {

        }

        public void RenderTerrain()
        {
            if (Terrain.Count == 0)
                return;

            var frustumPlanes = Camera.Current.GetFrustumPlanes();
            var visibleTiles = Terrain.Where(terrain => Camera.Current.IntersectsFrustum(terrain.BoundingBox.ToSphereBounds()))
                .OrderBy(terrain => (terrain.Transform.Position - Camera.Current.Position).LengthSquared)
                .ToList();
            Console.WriteLine(visibleTiles.Count);

            using var deugGroup = new DebugGroup("Terrain");
            foreach (var terrainPatch in visibleTiles)
                terrainPatch.Render();
        }
    }
}
