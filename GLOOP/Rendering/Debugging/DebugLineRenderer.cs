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

        private FastList<Vector3> Positions;
        private VAO EmptyVAO;
        private Buffer<Vector3> GPUBuffer;

        public DebugLineRenderer(int maxPoints)
        {
            EmptyVAO = new VAO(GL.GenVertexArray(), 0,0);
            Positions = new FastList<Vector3>(maxPoints);
            GPUBuffer = new Buffer<Vector3>(maxPoints, BufferTarget.ShaderStorageBuffer, BufferUsageHint.DynamicDraw, "Debug Line Points");

            PointsShader = new StaticPixelShader("assets/shaders/line/shader.vert", "assets/shaders/line/shader.frag", null, null);
        }

        public void AddLine(Vector3 start, Vector3 end)
        {
            Positions.Add(start);
            Positions.Add(end);
        }
        //public void AddLine(Vector4 start, Vector4 end)
        //{
        //    Positions.Add(start);
        //    Positions.Add(end);
        //}

        public void Render()
        {
            //GPUBuffer.Update(Positions.Elements, Positions.Count, 0);
            //GPUBuffer.Bind(1);

            PointsShader.Use();
            //EmptyVAO.NumIndicies = Positions.Count;
            //EmptyVAO.Draw(PrimitiveType.Lines);

            new Geometry()
            {
                Positions = Positions.Elements.ToList()
            }.ToVAO("test").Draw(PrimitiveType.Lines);

            Positions.Clear();
        }
    }
}
