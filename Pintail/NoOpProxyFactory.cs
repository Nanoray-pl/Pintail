using System;
using System.Diagnostics.CodeAnalysis;

namespace Nanoray.Pintail
{
    internal class NoOpProxyFactory<Context>: IProxyFactory<Context>
    {
        public ProxyInfo<Context> ProxyInfo { get; private set; }

        internal NoOpProxyFactory(ProxyInfo<Context> proxyInfo)
        {
            if (proxyInfo.Target.Type != proxyInfo.Proxy.Type)
                throw new ArgumentException($"{proxyInfo.Target.Type.GetShortName()} and {proxyInfo.Proxy.Type.GetShortName()} should be the same type.");
            this.ProxyInfo = proxyInfo;
        }

        public object ObtainProxy(IProxyManager<Context> manager, object targetInstance)
        {
            return targetInstance;
        }

        public bool TryUnproxy(IProxyManager<Context> manager, object potentialProxyInstance, [NotNullWhen(true)] out object? targetInstance)
        {
            targetInstance = potentialProxyInstance;
            return true;
        }
    }
}
