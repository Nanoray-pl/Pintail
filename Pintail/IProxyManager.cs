using System;

namespace Nanoray.Pintail
{
	public interface IProxyManager<Context> where Context: notnull, IEquatable<Context>
	{
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
    }
}
