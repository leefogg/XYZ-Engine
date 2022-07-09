using GLOOP.Util.Structures;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GLOOP.Rendering.Debugging
{
    public class DebugLineRenderer
    {
        private static StaticPixelShader PointsShader;
        private static VAOManager.VAOContainer VAOPool = VAOManager.Create(VAO.VAOShape.Lines, 0, sizeof(float) * 3 * 2 * 1024 * 128); // 128k lines

        private Geometry Geometry = new Geometry();
        private VirtualVAO VirtualVAO;

        static DebugLineRenderer()
        {
            PointsShader = new StaticPixelShader("assets/shaders/line/shader.vert", "assets/shaders/line/shader.frag", null, null);
        }

        public DebugLineRenderer(int maxLines)
        {
            Geometry.Positions = Enumerable.Repeat(Vector3.Zero, maxLines * 2).ToList();
            VirtualVAO = Geometry.ToVirtualVAO(VAOPool); // Put all LineRenderers in same VAO
        }

        public void AddLine(Vector3 start, Vector3 end)
        {
            Geometry.Positions.Add(start);
            Geometry.Positions.Add(end);
        }

        public void Render()
        {
            PointsShader.Use();
            Geometry.UpdateVAO(VirtualVAO);
            VirtualVAO.Draw(PrimitiveType.Lines);
            Geometry.Positions.Clear();
        }
    }
}
