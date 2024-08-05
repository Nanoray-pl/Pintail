using System;
using System.Diagnostics.CodeAnalysis;

namespace Nanoray.Pintail
{
    internal class EnumProxyFactory<Context>: IProxyFactory<Context>
    {
        public ProxyInfo<Context> ProxyInfo { get; private set; }

        internal EnumProxyFactory(ProxyInfo<Context> proxyInfo)
        {
            if (!proxyInfo.Target.Type.IsEnum)
                throw new ArgumentException($"{proxyInfo.Target.Type.GetShortName()} is not an enum");
            if (!proxyInfo.Proxy.Type.IsEnum)
                throw new ArgumentException($"{proxyInfo.Proxy.Type.GetShortName()} is not an enum");
            if (Enum.GetUnderlyingType(proxyInfo.Target.Type) != Enum.GetUnderlyingType(proxyInfo.Proxy.Type))
                throw new ArgumentException($"{proxyInfo.Target.Type.GetShortName()} and {proxyInfo.Proxy.Type.GetShortName()} have different underlying types");
            this.ProxyInfo = proxyInfo;
        }

        public object ObtainProxy(IProxyManager<Context> manager, object targetInstance)
        {
            return MapEnum(targetInstance, this.ProxyInfo.Proxy.Type);
        }

        public bool TryUnproxy(IProxyManager<Context> manager, object potentialProxyInstance, [NotNullWhen(true)] out object? targetInstance)
        {
            targetInstance = MapEnum(potentialProxyInstance, this.ProxyInfo.Target.Type);
            return true;
        }

        private static object MapEnum(object input, Type outputType)
            => Enum.ToObject(outputType, Convert.ChangeType(input, Enum.GetUnderlyingType(outputType)));
    }
}
