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
        public object? UnproxyOrObtainProxy(ProxyInfo<Context> proxyInfo, bool isReverse, object? toProxy)
        {
            if (toProxy is null)
                return null;
            ProxyInfo<Context> targetToProxyInfo = isReverse ? proxyInfo.Reversed() : proxyInfo;
            ProxyInfo<Context> proxyToTargetInfo = isReverse ? proxyInfo : proxyInfo.Reversed();

            var unproxyFactory = this.Manager.GetProxyFactory(proxyToTargetInfo);
            if (unproxyFactory is not null && unproxyFactory.TryUnproxy(this.Manager, toProxy, out object? targetInstance))
                return targetInstance;
            var factory = this.Manager.ObtainProxyFactory(targetToProxyInfo);
            return factory.ObtainProxy(this.Manager, toProxy);
        }

        public void MapArrayContents(ProxyInfo<Context> proxyInfo, bool isReverse, Array inputArray, Array outputArray)
        {
            ProxyInfo<Context> actualProxyInfo = isReverse ? proxyInfo.Reversed() : proxyInfo;
            var arrayProxyFactory = this.Manager.ObtainProxyFactory(actualProxyInfo) as DefaultArrayProxyFactory<Context> ?? throw new ArgumentException($"Could not obtain DefaultArrayProxyFactory for {actualProxyInfo}.");
            arrayProxyFactory.MapArrayContents(this.Manager, inputArray, outputArray);
        }
    }
}
