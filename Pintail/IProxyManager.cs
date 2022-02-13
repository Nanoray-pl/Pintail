using System;
using System.Diagnostics.CodeAnalysis;

namespace Nanoray.Pintail
{
    public interface IProxyManager<Context>
    {
        IProxyFactory<Context>? GetProxyFactory(ProxyInfo<Context> proxyInfo);
        IProxyFactory<Context> ObtainProxyFactory(ProxyInfo<Context> proxyInfo);
	}

    public static class NothingProxyManagerExtensions
    {
        [return: NotNullIfNotNull("instance")]
        public static TProxy? ObtainProxy<TProxy>(this IProxyManager<Nothing> self, object? instance) where TProxy : class
        {
            return self.ObtainProxy<Nothing, TProxy>(instance, Nothing.AtAll, Nothing.AtAll);
        }

        public static bool TryProxy<TProxy>(this IProxyManager<Nothing> self, object? toProxy, out TProxy? proxy) where TProxy: class
        {
            return self.TryProxy<Nothing, TProxy>(toProxy, Nothing.AtAll, Nothing.AtAll, out proxy);
        }
    }

    public static class ProxyManagerExtensions
    {
        [return: NotNullIfNotNull("instance")]
        public static TProxy? ObtainProxy<Context, TProxy>(this IProxyManager<Context> self, object? instance, Context targetContext, Context proxyContext) where TProxy: class
        {
            if (instance is null)
                return null;

            var factory = self.ObtainProxyFactory(new ProxyInfo<Context>(
                target: new TypeInfo<Context>(targetContext, instance.GetType()),
                proxy: new TypeInfo<Context>(proxyContext, typeof(TProxy))
            ));
            return (TProxy)factory.ObtainProxy(self, instance);
        }

        public static bool TryProxy<Context, TProxy>(this IProxyManager<Context> self, object? toProxy, Context targetContext, Context proxyContext, out TProxy? proxy) where TProxy: class
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
                proxy = (TProxy)proxyFactory.ObtainProxy(self, toProxy);
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
