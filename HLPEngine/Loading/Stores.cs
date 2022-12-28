using System;
using System.Collections.Generic;
using System.Text;
using GLOOP.HPL;

namespace HPLEngine.Loading
{
    public static class Stores
    {
        public static readonly Store DDS = new Store("DDS");
        public static readonly Store MAT = new Store("MAT");

        public static void Init()
        {
            DDS.Init(Constants.SOMARoot);
            DDS.WriteCache();
            MAT.Init(Constants.SOMARoot);
            MAT.WriteCache();
        }
    }
}
