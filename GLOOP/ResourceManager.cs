using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public static class ResourceManager
    {
        public static readonly List<IDisposable> Resources = new List<IDisposable>();

        public static void DisposeAll()
        {
            foreach (var resource in Resources)
                resource.Dispose();
        }

        internal static void Add(IDisposable resource)
        {
            if (!Resources.Contains(resource))
                Resources.Add(resource);
        }
    }
}
