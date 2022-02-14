using System;

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

        public Output MapEnum<Input, Output>(Input input) where Input: Enum where Output: Enum
        {
            foreach (object outputValue in Enum.GetValues(typeof(Output)))
            {
                var output = (Output)outputValue;
                if ((int)(object)output == (int)(object)input)
                    return output;
            }
            throw new ArgumentException($"Cannot map {input} to type {typeof(Output).GetBestName()}.");
        }
    }
}
