using System;

namespace Nanoray.Pintail.Tests
{
    public class ProviderApi
    {
        public void VoidMethod() { }

        public int IntMethod(int num)
            => num;

        public R MapperMethod<T, R>(T t, Func<T, R> mapper)
            => mapper(t);
    }
}
