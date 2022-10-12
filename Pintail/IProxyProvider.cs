using System.Diagnostics.CodeAnalysis;

namespace Nanoray.Pintail
{
    public interface IProxyProvider
    {
        bool CanProxy<TOriginal, TProxy>(TOriginal original, [NotNullWhen(true)] out IProxyProcessor<TOriginal, TProxy>? processor, IProxyProvider? rootProvider = null);

        IProxyProvider<TOriginal, TProxy> AsSpecificProxyProvider<TOriginal, TProxy>()
            => new GenericToSpecificProxyProvider<TOriginal, TProxy>(this);
    }

    public interface IProxyProvider<TOriginal, TProxy>
    {
        bool CanProxy(TOriginal original, [NotNullWhen(true)] out IProxyProcessor<TOriginal, TProxy>? processor, IProxyProvider? rootProvider = null);

        IProxyProvider AsGenericProxyProvider()
            => new SpecificToGenericProxyProvider<TOriginal, TProxy>(this);
    }
}