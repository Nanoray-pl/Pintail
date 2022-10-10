using System;
using System.Runtime.CompilerServices;

namespace Nanoray.Pintail
{
    public class NumericEnumProxyProvider : IProxyProvider
    {
        public bool CanProxy<TOriginal, TProxy>(TOriginal original, IProxyProvider? rootProvider)
            => typeof(TOriginal).IsEnum && typeof(TProxy).IsEnum && typeof(TOriginal).GetEnumUnderlyingType() == typeof(TProxy).GetEnumUnderlyingType();

        public TProxy ObtainProxy<TOriginal, TProxy>(TOriginal original, IProxyProvider? rootProvider)
            => Unsafe.As<TOriginal, TProxy>(ref original);
    }
}