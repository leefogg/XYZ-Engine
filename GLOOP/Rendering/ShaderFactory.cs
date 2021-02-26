using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class ShaderFactory<T> where T : Shader
    {
        private T[] shaders;

        public ShaderFactory(Func<IDictionary<string,string>, T> provider, string[] allDefines) {
            shaders = new T[(int)Math.Pow(2, allDefines.Length)];

            for (int i = 0; i < shaders.Length; i++)
            {
                var defines = new Dictionary<string, string>();
                for (int j = 0; j < allDefines.Length; j++)
                    defines.Add(allDefines[j], IsBitSet(i, j) ? "1" : "0");

                shaders[i] = provider(defines);
            }
        }

        public T GetVarient(params bool[] enabledDefines)
        {
            var index = 0;
            var k = 1;
            for (int j = 0; j < enabledDefines.Length; j++, k <<= 1)
                if (enabledDefines[j])
                    index |= k;

            return shaders[index];
        }

        private bool IsBitSet(int b, int pos) => (b & (1 << pos)) != 0;
    }
}
