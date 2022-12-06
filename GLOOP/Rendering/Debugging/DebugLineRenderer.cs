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
        private static StaticPixelShader LineShader;
        private static VAOManager.VAOContainer VAOPool = VAOManager.Create(VAO.VAOShape.Lines, 0, sizeof(float) * 3 * 2 * 1024 * 128); // 128k lines

        private Geometry Geometry = new Geometry();
        private VirtualVAO VirtualVAO;

        static DebugLineRenderer()
        {
            LineShader = new StaticPixelShader("assets/shaders/line/shader.vert", "assets/shaders/line/shader.frag", null, null);
        }

        public DebugLineRenderer(int initialLines)
        {
            Geometry.Positions = Enumerable.Repeat(Vector3.Zero, initialLines * 2).ToList();
            VirtualVAO = Geometry.ToVirtualVAO(VAOPool); // Put all LineRenderers in same VAO
            Geometry.Positions.Clear();
        }

        public void AddLine(Vector3 start, Vector3 end)
        {
            if (Geometry.Positions.Count + 1 >= Geometry.Positions.Capacity)
                return;

            Geometry.Positions.Add(start);
            Geometry.Positions.Add(end);
        }

        public void Render()
        {
            // Hack to avoid redrawing previous frame's lines
            // TODO: Use circular buffer
            Geometry.Positions.AddRange(Enumerable.Repeat(Vector3.Zero, Geometry.Positions.Capacity - Geometry.Positions.Count));

            LineShader.Use();
            Geometry.UpdateVAO(VirtualVAO);
            VirtualVAO.Draw(PrimitiveType.Lines);

            Geometry.Positions.Clear();
        }

        public void AddAxisHelper(Matrix4 modelMatrix)
        {
            var O = (new Vector4(0, 0, 0, 1) * modelMatrix).Xyz;
            var Y = (new Vector4(1, 0, 0, 1) * modelMatrix).Xyz;
            var X = (new Vector4(0, 1, 0, 1) * modelMatrix).Xyz;
            var Z = (new Vector4(0, 0, 1, 1) * modelMatrix).Xyz;

            AddLine(O, X);
            AddLine(O, Y);
            AddLine(O, Z);
        }

        public void DrawPlane(int width, int depth, int rows, int columns, Vector3 offset = default)
        {
            var topLeft = new Vector3(-width / 2, 0, -depth / 2);
            var topRight = new Vector3(width / 2, 0, -depth / 2);
            {
                var zStep = (float)depth / rows;
                var hLine = new Vector3(0, 0, zStep);
                for (float row = 0; row < rows; row++)
                    AddLine(offset + topLeft + hLine * row, offset + topRight + hLine * row);
            }
            var bottomLeft = new Vector3(-width / 2, 0, depth / 2);
            var bottomRight = new Vector3(width / 2, 0, depth / 2);
            {
                var xStep = (float)width / columns;
                var yLine = new Vector3(xStep, 0, 0);
                for (float column = 0; column < columns; column++)
                    AddLine(offset + bottomLeft + yLine * column, offset + topLeft + yLine * column);
            }

            AddLine(offset + topRight, offset + bottomRight);
            AddLine(offset + bottomLeft, offset + bottomRight);
        }
    }
}
