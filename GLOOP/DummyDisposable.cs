﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public class DummyDisposable : IDisposable
    {
        public static readonly IDisposable Instance = default(DummyDisposable);

        public virtual void Dispose() { }
    }
}
