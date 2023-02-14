namespace Nanoray.Pintail
{
    public class CachingProxyProcessor<TOriginal, TProxy> : IProxyProcessor<TOriginal, TProxy>
    {
        public double Priority
            => this.Wrapped.Priority;

        public TOriginal Original
            => this.Wrapped.Original;

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
            lock (this.Lock)
            {
                if (this.IsCached)
                    return this.CachedProxy!;

                var proxy = this.Wrapped.ObtainProxy();
                this.CachedProxy = proxy;
                this.IsCached = true;
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
