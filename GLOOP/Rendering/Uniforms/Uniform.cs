using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering.Uniforms
{
    public abstract class Uniform
    {
        public readonly int location = -1;

        public Uniform(Shader shader, string uniformName)
        {
            if (shader.TryGetUniformLocaiton(uniformName, out var loc))
                location = loc;
            else
                Console.WriteLine($"Could not find uniform {uniformName}.");
        }
    }
}
