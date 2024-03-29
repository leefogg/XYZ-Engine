﻿using GLOOP.Rendering.Materials;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Debugging
{
    public class Draw
    {
        private static readonly SingleColorMaterial boundingBoxMaterial = new SingleColorMaterial(Shader.SingleColorShader);

        public static void BoundingBox(Matrix4 modelMatrix, Vector4 color)
        {
            boundingBoxMaterial.SetModelMatrix( modelMatrix);
            boundingBoxMaterial.Color = color;
            boundingBoxMaterial.Commit();
            Primitives.WireframeCube.Draw(PrimitiveType.Lines);
        }

        public static void Box(Matrix4 modelMatrix, Vector4 color)
        {
            boundingBoxMaterial.SetModelMatrix(modelMatrix);
            boundingBoxMaterial.Color = color;
            boundingBoxMaterial.Commit();
            Primitives.Cube.Draw();
        }
    }
}
