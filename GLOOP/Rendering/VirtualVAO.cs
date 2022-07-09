using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class VirtualVAO : IDrawable
    {
        public readonly DrawElementsIndirectData Description;
        public readonly VAO Container;
        public Box3 BoundingBox;

        public VirtualVAO(DrawElementsIndirectData description, VAO container)
        {
            Description = description;
            Container = container;
        }

        public void Draw(PrimitiveType renderMode = PrimitiveType.Triangles, int numInstances = 1)
        {
            Container.Bind();
            //if (Container.NumIndicies == 0)
            //    GL.DrawArraysInstancedBaseInstance(
            //        renderMode,
            //        (int)Description.FirstIndex,
            //        (int)Description.BaseVertex,
            //        numInstances ?? (int)Description.NumInstances,
            //        (int)Description.BaseInstance
            //    );
            //else
                GL.DrawElementsInstancedBaseVertexBaseInstance(
                    renderMode,
                    (int)Description.NumIndexes,
                    DrawElementsType.UnsignedShort,
                    (IntPtr)Description.FirstIndex,
                    numInstances,
                    (int)Description.BaseVertex,
                    (int)Description.BaseInstance
                );
            
            Metrics.ModelsDrawn++;
        }
    }
}
