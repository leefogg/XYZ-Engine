using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public static class VAOCache
    {
        private static Dictionary<string, VirtualVAO> VAOs = new Dictionary<string, VirtualVAO>();

        public static bool Get(string path, out VirtualVAO vao)
        {
            if (VAOs.TryGetValue(path, out vao))
                return true;

            vao = null;
            return false;
        }

        public static void Put(VirtualVAO newvao, string name)
        {
            VAOs[name] = newvao;
        }
    }
}
