using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Util.Structures
{
    public class Lazy<T>
    {
        private Func<T> func;

        private bool isExpired;
        public bool IsExpired => isExpired;
        private bool hasValue = false;
        private T value;
        public T Value
        {
            get
            {
                if (!hasValue || IsExpired)
                {
                    value = func();
                    isExpired = false;
                    hasValue = true;
                }
                return value;
            }
        }

        public Lazy(Func<T> func)
        {
            this.func = func;
        }

        public void Expire()
        {
            isExpired = true;
        }

        public static implicit operator T(Lazy<T> self) => self.Value;
    }
}
