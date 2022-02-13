namespace Nanoray.Pintail
{
    public sealed class DefaultProxyGlue<Context>
    {
        private readonly IProxyManager<Context> Manager;

        internal DefaultProxyGlue(IProxyManager<Context> manager)
        {
            this.Manager = manager;
        }

        public object? ObtainProxy(ProxyInfo<Context> proxyInfo, object? toProxy)
        {
            if (toProxy is null)
                return null;
            var factory = this.Manager.ObtainProxyFactory(proxyInfo);
            return factory.ObtainProxy(this.Manager, toProxy);
        }

        public object? UnproxyOrObtainProxy(ProxyInfo<Context> proxyInfo, ProxyInfo<Context> unproxyInfo, object? toProxy)
        {
            if (toProxy is null)
                return null;
            var unproxyFactory = this.Manager.GetProxyFactory(unproxyInfo);
            if (unproxyFactory is not null && unproxyFactory.TryUnproxy(toProxy, out object? targetInstance))
                return targetInstance;
            return this.ObtainProxy(proxyInfo, toProxy);
        }
    }
}
