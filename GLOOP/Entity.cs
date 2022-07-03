using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public abstract class Entity
    {
        public abstract DynamicTransform Transform { get; set; }
    }
}
