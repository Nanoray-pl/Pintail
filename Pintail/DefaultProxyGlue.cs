using System;
using System.Diagnostics.CodeAnalysis;

namespace Nanoray.Pintail
{
    /// <summary>
    /// A "glue" worker instance used by proxy instances to do any proxying-related tasks.
    /// </summary>
    /// <remarks>This type is not meant to be used directly, but due to the nature of the proxy objects it has to be `public`.</remarks>
    /// <typeparam name="Context"></typeparam>
    public sealed class DefaultProxyGlue<Context>
    {
        private readonly IProxyManager<Context> Manager;

        internal DefaultProxyGlue(IProxyManager<Context> manager)
        {
            this.Manager = manager;
        }

        [return: NotNullIfNotNull("toProxy")]
        public object? ObtainProxy(ProxyInfo<Context> proxyInfo, object? toProxy)
        {
            if (toProxy is null)
                return null;
            var factory = this.Manager.ObtainProxyFactory(proxyInfo);
            return factory.ObtainProxy(this.Manager, toProxy);
        }

        [return: NotNullIfNotNull("toProxy")]
        public object? UnproxyOrObtainProxy(ProxyInfo<Context> proxyInfo, ProxyInfo<Context> unproxyInfo, object? toProxy)
        {
            if (toProxy is null)
                return null;
            var unproxyFactory = this.Manager.GetProxyFactory(unproxyInfo);
            if (unproxyFactory is not null && unproxyFactory.TryUnproxy(this.Manager, toProxy, out object? targetInstance))
                return targetInstance;
            return this.ObtainProxy(proxyInfo, toProxy);
        }

        public void MapArrayContents(ProxyInfo<Context> proxyInfo, Array inputArray, Array outputArray)
        {
            var arrayProxyFactory = this.Manager.ObtainProxyFactory(proxyInfo) as DefaultArrayProxyFactory<Context> ?? throw new ArgumentException($"Could not obtain DefaultArrayProxyFactory for {proxyInfo}.");
            arrayProxyFactory.MapArrayContents(this.Manager, inputArray, outputArray);
        }
    }
}
