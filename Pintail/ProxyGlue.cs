using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Nanoray.Pintail
{
    /// <summary>
    /// A "glue" worker instance used by proxy instances to do any proxying-related tasks.
    /// </summary>
    /// <remarks>This type is not meant to be used directly, but due to the nature of the proxy objects it has to be `public`.</remarks>
    /// <typeparam name="Context"></typeparam>
    public sealed class ProxyGlue<Context>
    {
        private readonly IProxyManager<Context> Manager;

        internal ProxyGlue(IProxyManager<Context> manager)
        {
            this.Manager = manager;
        }

        [return: NotNullIfNotNull("toProxy")]
#pragma warning disable CS1591
        public object? UnproxyOrObtainProxy(IDictionary<string, Type> targetGenericArguments, IDictionary<string, Type> proxyGenericArguments, ProxyInfo<Context> proxyInfo, bool isReverse, object? toProxy)
#pragma warning restore CS1591
        {
            if (toProxy is null)
                return null;

            ProxyInfo<Context> targetToProxyInfo = isReverse ? proxyInfo.Reversed() : proxyInfo;
            ProxyInfo<Context> proxyToTargetInfo = isReverse ? proxyInfo : proxyInfo.Reversed();

            targetToProxyInfo = targetToProxyInfo.Copy(
                targetType: targetToProxyInfo.Target.Type.ReplacingGenericArguments(targetGenericArguments),
                proxyType: targetToProxyInfo.Proxy.Type.ReplacingGenericArguments(proxyGenericArguments)
            );
            proxyToTargetInfo = proxyToTargetInfo.Copy(
                targetType: proxyToTargetInfo.Target.Type.ReplacingGenericArguments(targetGenericArguments),
                proxyType: proxyToTargetInfo.Proxy.Type.ReplacingGenericArguments(proxyGenericArguments)
            );

            var unproxyFactory = this.Manager.GetProxyFactory(proxyToTargetInfo);
            if (unproxyFactory is not null && unproxyFactory.TryUnproxy(this.Manager, toProxy, out object? targetInstance))
                return targetInstance;
            var factory = this.Manager.ObtainProxyFactory(targetToProxyInfo);
            return factory.ObtainProxy(this.Manager, toProxy);
        }

#pragma warning disable CS1591
        public void MapArrayContents(ProxyInfo<Context> proxyInfo, bool isReverse, Array inputArray, Array outputArray)
#pragma warning restore CS1591
        {
            ProxyInfo<Context> actualProxyInfo = isReverse ? proxyInfo.Reversed() : proxyInfo;
            var arrayProxyFactory = this.Manager.ObtainProxyFactory(actualProxyInfo) as ArrayProxyFactory<Context> ?? throw new ArgumentException($"Could not obtain DefaultArrayProxyFactory for {actualProxyInfo}.");
            arrayProxyFactory.MapArrayContents(this.Manager, inputArray, outputArray);
        }
    }
}
