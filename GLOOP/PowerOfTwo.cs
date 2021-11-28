using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public enum PowerOfTwo : int
    {
        Zero = 0,
        One = 1 << 0,
        Two = 1 << 1,
        Four = 1 << 2,
        Eight = 1 << 3,
        Sixteen = 1 << 4,
        ThrirtyTwo = 1 << 5,
        SixtyFour = 1 << 6,
        OneHundrendAndTwentyEight = 1 << 7,
    }
}
