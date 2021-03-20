using GLOOP.Rendering.Materials;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Debugging
{
    public class Draw
    {
        private static readonly SingleColorMaterial boundingBoxMaterial = new SingleColorMaterial(Shader.SingleColorShader) { Color = new Vector4(1) };

        public static void BoundingBox(Matrix4 projectionMatrix, Matrix4 viewMatrix, Matrix4 modelMatrix, Vector4 color)
        {
            boundingBoxMaterial.SetCameraUniforms(projectionMatrix, viewMatrix, modelMatrix);
            boundingBoxMaterial.Color = color;
            boundingBoxMaterial.Commit();
            Primitives.WireframeCube.Draw(PrimitiveType.Lines);
        }

        public static void Box(Matrix4 projectionMatrix, Matrix4 viewMatrix, Matrix4 modelMatrix, Vector4 color)
        {
            boundingBoxMaterial.SetCameraUniforms(projectionMatrix, viewMatrix, modelMatrix);
            boundingBoxMaterial.Color = color;
            boundingBoxMaterial.Commit();
            Primitives.Cube.Draw();
        }
    }
}
