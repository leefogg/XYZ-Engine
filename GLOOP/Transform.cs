﻿using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public interface Transform
    {
        public abstract Matrix4 Matrix { get; }
    }
}
