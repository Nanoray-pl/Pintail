using System;

namespace Nanoray.Pintail
{
    public class CachingProxyProcessor<TOriginal, TProxy> : IProxyProcessor<TOriginal, TProxy>
    {
        public double Priority
            => Wrapped.Priority;

        public TOriginal Original
            => Wrapped.Original;

        private IProxyProcessor<TOriginal, TProxy> Wrapped { get; init; }
        private object Lock { get; init; } = new object();
        private bool IsCached { get; set; } = false;
        private TProxy? CachedProxy { get; set; } = default;

        public CachingProxyProcessor(IProxyProcessor<TOriginal, TProxy> wrapped)
        {
            this.Wrapped = wrapped;
        }

        public TProxy ObtainProxy()
        {
            lock (Lock)
            {
                if (IsCached)
                    return CachedProxy!;

                var proxy = Wrapped.ObtainProxy();
                CachedProxy = proxy;
                IsCached = true;
                return proxy;
            }
        }
    }

    public static class CachingProxyProcessorExt
    {
        public static CachingProxyProcessor<TOriginal, TProxy> Caching<TOriginal, TProxy>(this IProxyProcessor<TOriginal, TProxy> processor)
            => (processor as CachingProxyProcessor<TOriginal, TProxy>) ?? new(processor);
    }
}