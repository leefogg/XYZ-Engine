using GLOOP.Rendering;
using GLOOP.Rendering.Uniforms;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Tests.Assets.Shaders
{
    public class FrustumShader : StaticPixelShader
    {
        public readonly Uniform16f modelMatrix;
        private Uniform3f scale;
        private Uniform1f aspectRatio;

        public Matrix4 ModelMatrix
        {
            set => modelMatrix.Set(value);
        }
        public Vector3 Scale
        {
            set => scale.Set(value);
        }
        public float AspectRatio
        {
            set => aspectRatio.Set(value);
        }

        public FrustumShader(IDictionary<string, string> defines = null, string name = null)
           : base("tests/assets/shaders/frustum.vert",
                  "tests/assets/shaders/frustum.frag", defines, name)
        {
            modelMatrix = new Uniform16f(this, "ModelMatrix");

            scale = new Uniform3f(this, "scale");
            aspectRatio = new Uniform1f(this, "ar");
        }
    }
}
