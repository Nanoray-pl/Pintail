using System;
using System.Diagnostics.CodeAnalysis;

namespace Nanoray.Pintail
{
    public class SpecificToGenericProxyProvider<TSpecificOriginal, TSpecificProxy> : IProxyProvider
    {
        private IProxyProvider<TSpecificOriginal, TSpecificProxy> Wrapped { get; }

        public SpecificToGenericProxyProvider(IProxyProvider<TSpecificOriginal, TSpecificProxy> wrapped)
        {
            this.Wrapped = wrapped;
        }

        bool IProxyProvider.CanProxy<TGenericOriginal, TGenericProxy>(TGenericOriginal original, [NotNullWhen(true)] out IProxyProcessor<TGenericOriginal, TGenericProxy>? processor, IProxyProvider? rootProvider)
        {
            processor = null;
            if (!typeof(TGenericOriginal).IsAssignableTo(typeof(TSpecificOriginal)))
                return false;
            if (!typeof(TGenericProxy).IsAssignableFrom(typeof(TSpecificProxy)))
                return false;

            bool result = Wrapped.CanProxy((TSpecificOriginal)(object)original, out var specificProcessor, rootProvider);
            if (result && specificProcessor is not null)
                processor = (IProxyProcessor<TGenericOriginal, TGenericProxy>)specificProcessor;
            return result;
        }
    }
}