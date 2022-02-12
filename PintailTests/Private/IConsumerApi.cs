using System;

namespace Nanoray.Pintail.Tests
{
    public interface IConsumerApi
    {
        void VoidMethod();
        int IntMethod(int num);
        R MapperMethod<T, R>(T t, Func<T, R> mapper);
    }
}
