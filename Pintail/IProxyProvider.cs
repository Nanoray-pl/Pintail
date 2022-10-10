using System;

namespace Nanoray.Pintail
{
    public interface IProxyProvider
    {
        bool CanProxy<TOriginal, TProxy>(TOriginal original, IProxyProvider? rootProvider = null);
        TProxy ObtainProxy<TOriginal, TProxy>(TOriginal original, IProxyProvider? rootProvider = null);

        IProxyProvider<TOriginal, TProxy> AsSpecificProxyProvider<TOriginal, TProxy>()
            => new GenericToSpecificProxyProvider<TOriginal, TProxy>(this);
    }

    public interface IProxyProvider<TOriginal, TProxy>
    {
        bool CanProxy(TOriginal original, IProxyProvider? rootProvider = null);
        TProxy ObtainProxy(TOriginal original, IProxyProvider? rootProvider = null);

        IProxyProvider AsGenericProxyProvider()
            => new SpecificToGenericProxyProvider<TOriginal, TProxy>(this);
    }
}