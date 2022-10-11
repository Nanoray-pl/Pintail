using System;
using System.Diagnostics.CodeAnalysis;

namespace Nanoray.Pintail
{
    public class GenericToSpecificProxyProvider<TSpecificOriginal, TSpecificProxy> : IProxyProvider<TSpecificOriginal, TSpecificProxy>
    {
        private IProxyProvider Wrapped { get; }

        public GenericToSpecificProxyProvider(IProxyProvider wrapped)
        {
            this.Wrapped = wrapped;
        }

        bool IProxyProvider<TSpecificOriginal, TSpecificProxy>.CanProxy(TSpecificOriginal original, [NotNullWhen(true)] out IProxyProcessor<TSpecificOriginal, TSpecificProxy>? processor, IProxyProvider? rootProvider)
            => Wrapped.CanProxy<TSpecificOriginal, TSpecificProxy>(original, out processor, rootProvider);
    }
}