using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Materials
{
    public class SingleColorMaterial : Material
    {
        public Vector4 Color;
        private SingleColorShader3D shader;

        public override Shader Shader => shader;

        public SingleColorMaterial(SingleColorShader3D shader)
        {
            this.shader = shader;
        }

        public override void Commit()
        {
            shader.Use();
            shader.ModelMatrix = ModelMatrix;
            shader.Color = Color;
        }

        public override Material Clone()
        {
            return new SingleColorMaterial(shader)
            {
                Color = Color
            };
        }
    }
}
