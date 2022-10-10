using System;
using System.Runtime.CompilerServices;

namespace Nanoray.Pintail
{
    public class SameTypeProxyProvider : IProxyProvider
    {
        public bool CanProxy<TOriginal, TProxy>(TOriginal original, IProxyProvider? rootProvider)
            => typeof(TOriginal) == typeof(TProxy);

        public TProxy ObtainProxy<TOriginal, TProxy>(TOriginal original, IProxyProvider? rootProvider)
            => Unsafe.As<TOriginal, TProxy>(ref original);
    }
}