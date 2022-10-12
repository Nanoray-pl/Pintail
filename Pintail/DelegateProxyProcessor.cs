using System;

namespace Nanoray.Pintail
{
    public sealed class DelegateProxyProcessor<TOriginal, TProxy> : IProxyProcessor<TOriginal, TProxy>
    {
        public double Priority { get; private init; }
        public TOriginal Original { get; private init; }
        private Func<TOriginal, TProxy> ProxyDelegate { get; }

        public DelegateProxyProcessor(double priority, TOriginal original, Func<TOriginal, TProxy> proxyDelegate)
        {
            this.Priority = priority;
            this.Original = original;
            this.ProxyDelegate = proxyDelegate;
        }

        public TProxy ObtainProxy()
            => this.ProxyDelegate(this.Original);
    }
}