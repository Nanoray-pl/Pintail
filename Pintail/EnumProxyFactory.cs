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
                throw new ArgumentException($"{proxyInfo.Target.Type.GetShortName()} is not an enum.");
            if (!proxyInfo.Proxy.Type.IsEnum)
                throw new ArgumentException($"{proxyInfo.Proxy.Type.GetShortName()} is not an enum.");
            this.ProxyInfo = proxyInfo;
        }

        public object ObtainProxy(IProxyManager<Context> manager, object targetInstance)
        {
            return this.MapEnum(targetInstance, this.ProxyInfo.Proxy.Type);
        }

        public bool TryUnproxy(IProxyManager<Context> manager, object potentialProxyInstance, [NotNullWhen(true)] out object? targetInstance)
        {
            targetInstance = this.MapEnum(potentialProxyInstance, this.ProxyInfo.Target.Type);
            return true;
        }

        private object MapEnum(object input, Type outputType)
        {
            foreach (object outputValue in Enum.GetValues(outputType))
            {
                if ((int)outputValue == (int)input)
                    return outputValue;
            }
            throw new ArgumentException($"Cannot map {input} to type {outputType.GetShortName()}.");
        }
    }
}
