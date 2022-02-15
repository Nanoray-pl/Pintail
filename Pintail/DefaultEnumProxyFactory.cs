using System;
using System.Diagnostics.CodeAnalysis;

namespace Nanoray.Pintail
{
    internal class DefaultEnumProxyFactory<Context>: IProxyFactory<Context>
    {
        public ProxyInfo<Context> ProxyInfo { get; private set; }

        internal DefaultEnumProxyFactory(ProxyInfo<Context> proxyInfo)
        {
            this.ProxyInfo = proxyInfo;
        }

        public object ObtainProxy(IProxyManager<Context> manager, object targetInstance)
        {
            if (!this.ProxyInfo.Target.Type.IsEnum)
                throw new ArgumentException($"{this.ProxyInfo.Target.Type.GetBestName()} is not an enum.");
            if (!this.ProxyInfo.Proxy.Type.IsEnum)
                throw new ArgumentException($"{this.ProxyInfo.Proxy.Type.GetBestName()} is not an enum.");
            return this.MapEnum(targetInstance, this.ProxyInfo.Proxy.Type);
        }

        public bool TryUnproxy(IProxyManager<Context> manager, object potentialProxyInstance, [NotNullWhen(true)] out object? targetInstance)
        {
            if (!this.ProxyInfo.Target.Type.IsEnum)
                throw new ArgumentException($"{this.ProxyInfo.Target.Type.GetBestName()} is not an enum.");
            if (!this.ProxyInfo.Proxy.Type.IsEnum)
                throw new ArgumentException($"{this.ProxyInfo.Proxy.Type.GetBestName()} is not an enum.");
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
            throw new ArgumentException($"Cannot map {input} to type {outputType.GetBestName()}.");
        }
    }
}
