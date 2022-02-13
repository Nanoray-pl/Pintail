using System;

namespace Nanoray.Pintail
{
    public interface IProxyManager<Context> where Context: notnull, IEquatable<Context>
    {
        IProxyFactory<Context>? GetProxyFactory(ProxyInfo<Context> proxyInfo);
        IProxyFactory<Context> ObtainProxyFactory(ProxyInfo<Context> proxyInfo);
	}

    public static class ProxyManagerExtensions
    {
        public static TProxy? ObtainProxy<Context, TProxy>(this IProxyManager<Context> self, object? instance, Context targetContext, Context proxyContext) where Context: notnull, IEquatable<Context> where TProxy : class
        {
            if (instance is null)
                return null;

            var factory = self.ObtainProxyFactory(new ProxyInfo<Context>(
                target: new TypeInfo<Context>(targetContext, instance.GetType()),
                proxy: new TypeInfo<Context>(proxyContext, typeof(TProxy))
            ));
            return (TProxy?)factory.ObtainProxy(self, instance);
        }

        public static bool TryProxy<Context, TProxy>(this IProxyManager<Context> self, object? toProxy, Context targetContext, Context proxyContext, out TProxy? proxy) where Context : notnull, IEquatable<Context> where TProxy : class
        {
            if (toProxy is null)
            {
                proxy = null;
                return true;
            }

            try
            {
                foreach (Type interfaceType in toProxy.GetType().GetInterfacesRecursively(includingSelf: true))
                {
                    var unproxyFactory = self.GetProxyFactory(new ProxyInfo<Context>(
                        target: new TypeInfo<Context>(targetContext, typeof(TProxy)),
                        proxy: new TypeInfo<Context>(proxyContext, interfaceType)
                    ));
                    if (unproxyFactory is null)
                        continue;
                    if (unproxyFactory.TryUnproxy(toProxy, out object? targetInstance))
                    {
                        proxy = (TProxy?)targetInstance;
                        return true;
                    }
                }

                var proxyFactory = self.ObtainProxyFactory(new ProxyInfo<Context>(
                    target: new TypeInfo<Context>(targetContext, toProxy.GetType()),
                    proxy: new TypeInfo<Context>(proxyContext, typeof(TProxy))
                ));
                proxy = (TProxy?)proxyFactory.ObtainProxy(self, toProxy);
                return true;
            }
            catch
            {
                proxy = null;
                return false;
            }
        }
    }
}
