using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable CS1591
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
        public object? UnproxyOrObtainProxy(Dictionary<string, Type>? targetGenericArguments, Dictionary<string, Type>? proxyGenericArguments, ProxyInfo<Context> proxyInfo, bool isReverse, object? toProxy)
        {
            if (toProxy is null)
                return null;

            var targetToProxyInfo = isReverse ? proxyInfo.Reversed() : proxyInfo;
            var proxyToTargetInfo = isReverse ? proxyInfo : proxyInfo.Reversed();

            targetToProxyInfo = targetToProxyInfo.Copy(
                targetType: targetToProxyInfo.Target.Type.ReplacingGenericArguments(targetGenericArguments),
                proxyType: targetToProxyInfo.Proxy.Type.ReplacingGenericArguments(proxyGenericArguments)
            );
            proxyToTargetInfo = proxyToTargetInfo.Copy(
                targetType: proxyToTargetInfo.Target.Type.ReplacingGenericArguments(targetGenericArguments),
                proxyType: proxyToTargetInfo.Proxy.Type.ReplacingGenericArguments(proxyGenericArguments)
            );

            while (true)
            {
                if (targetToProxyInfo.Proxy.Type.IsInstanceOfType(toProxy))
                    return toProxy;
                if (toProxy is not IInternalProxyObject internalProxyObject)
                    break;
                toProxy = internalProxyObject.ProxyTargetInstance;
            }

            var unproxyFactory = this.Manager.GetProxyFactory(proxyToTargetInfo);
            if (unproxyFactory is not null && unproxyFactory.TryUnproxy(this.Manager, toProxy, out object? targetInstance))
                return targetInstance;

            if (!targetToProxyInfo.Target.Type.IsInstanceOfType(toProxy))
            {
                var intermediateProxyInfo = targetToProxyInfo.Copy(
                    targetType: toProxy.GetType(),
                    proxyType: targetToProxyInfo.Target.Type
                );
                var intermediateFactory = this.Manager.ObtainProxyFactory(intermediateProxyInfo);
                toProxy = intermediateFactory.ObtainProxy(this.Manager, toProxy);
            }

            var factory = this.Manager.ObtainProxyFactory(targetToProxyInfo);
            return factory.ObtainProxy(this.Manager, toProxy);
        }

        public void MapArrayContents(ProxyInfo<Context> proxyInfo, bool isReverse, Array inputArray, Array outputArray)
        {
            var actualProxyInfo = isReverse ? proxyInfo.Reversed() : proxyInfo;
            var arrayProxyFactory = this.Manager.ObtainProxyFactory(actualProxyInfo) as ArrayProxyFactory<Context> ?? throw new ArgumentException($"Could not obtain DefaultArrayProxyFactory for {actualProxyInfo}.");
            arrayProxyFactory.MapArrayContents(this.Manager, inputArray, outputArray);
        }
    }
}
#pragma warning restore CS1591
