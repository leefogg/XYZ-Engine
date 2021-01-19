using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public class Lazy<T> where T : struct
    {
        private Func<T> func;

        private bool isExpired;
        public bool IsExpired => isExpired;

        private T? value;
        public T Value
        {
            get
            {
                if (!IsValueCreated || IsExpired)
                {
                    value = func();
                    isExpired = false;
                }
                return value.Value;
            }
        }
        public bool IsValueCreated => value.HasValue;


        public Lazy(Func<T> func)
        {
            this.func = func;
        }

        public void Expire()
        {
            isExpired = true;
        }
    }
}
