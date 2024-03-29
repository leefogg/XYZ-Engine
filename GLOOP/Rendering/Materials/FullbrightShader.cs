﻿using GLOOP.Rendering.Uniforms;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Materials
{
    public class FullbrightShader : StaticPixelShader
    {
        public readonly Uniform16f modelMatrix;
        public readonly UniformTexture diffuse;

        public TextureUnit DiffuseTexture
        {
            set => diffuse.Set(value);
        }
       
        public Matrix4 ModelMatrix
        {
            set => modelMatrix.Set(value);
        }

        public FullbrightShader() 
            : base("assets/shaders/FullBright/3D/VertexShader.vert", "assets/shaders/FullBright/3D/FragmentShader.frag", null, "Fullbright")
        {
            diffuse = new UniformTexture(this, "texture0");
            modelMatrix = new Uniform16f(this, "ModelMatrix");
        }
    }
}
