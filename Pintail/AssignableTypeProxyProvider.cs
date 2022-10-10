using System;
using System.Runtime.CompilerServices;

namespace Nanoray.Pintail
{
    public sealed class AssignableTypeProxyProvider : IProxyProvider
    {
        public bool CanProxy<TOriginal, TProxy>(TOriginal original, IProxyProvider? rootProvider)
            => typeof(TOriginal).IsAssignableTo(typeof(TProxy));

        public TProxy ObtainProxy<TOriginal, TProxy>(TOriginal original, IProxyProvider? rootProvider)
            => Unsafe.As<TOriginal, TProxy>(ref original);
    }
}