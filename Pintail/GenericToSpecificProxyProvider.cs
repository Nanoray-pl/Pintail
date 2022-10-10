using System;

namespace Nanoray.Pintail
{
    public class GenericToSpecificProxyProvider<TSpecificOriginal, TSpecificProxy> : IProxyProvider<TSpecificOriginal, TSpecificProxy>
    {
        private IProxyProvider Wrapped { get; }

        public GenericToSpecificProxyProvider(IProxyProvider wrapped)
        {
            this.Wrapped = wrapped;
        }

        public bool CanProxy(TSpecificOriginal original, IProxyProvider? rootProvider)
            => Wrapped.CanProxy<TSpecificOriginal, TSpecificProxy>(original, rootProvider);

        public TSpecificProxy ObtainProxy(TSpecificOriginal original, IProxyProvider? rootProvider)
            => Wrapped.ObtainProxy<TSpecificOriginal, TSpecificProxy>(original, rootProvider);
    }
}