using System.Diagnostics.CodeAnalysis;

namespace Nanoray.Pintail
{
    public interface IProxyFactory<Context>
	{
        ProxyInfo<Context> ProxyInfo { get; }

        [return: NotNullIfNotNull("targetInstance")] object? ObtainProxy(IProxyManager<Context> manager, object? targetInstance);
        bool TryUnproxy(object? potentialProxyInstance, out object? targetInstance);
    }
}
