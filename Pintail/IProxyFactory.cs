using System;

namespace Nanoray.Pintail
{
	public interface IProxyFactory<Context> where Context: notnull, IEquatable<Context>
	{
        ProxyInfo<Context> ProxyInfo { get; }

        object? ObtainProxy(IProxyManager<Context> manager, object? targetInstance);
        bool TryUnproxy(object potentialProxyInstance, out object? targetInstance);
    }
}
