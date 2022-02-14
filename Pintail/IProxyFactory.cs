using System.Diagnostics.CodeAnalysis;

namespace Nanoray.Pintail
{
    public interface IProxyFactory<Context>
	{
        /// <summary>
        /// The proxy information describing this specific <see cref="IProxyFactory{}"/>.
        /// </summary>
        ProxyInfo<Context> ProxyInfo { get; }

        /// <summary>
        /// Returns a proxy instance for a given instance.
        /// </summary>
        /// <param name="manager">The <see cref="IProxyManager{}"/> managing this <see cref="IProxyFactory{}"/>.</param>
        /// <param name="targetInstance">The instance to create a proxy for.</param>
        /// <returns>A proxy of the given instance.</returns>
        [return: NotNullIfNotNull("targetInstance")]
        object? ObtainProxy(IProxyManager<Context> manager, object? targetInstance);

        /// <summary>
        /// Tries to unproxy a given instance.
        /// </summary>
        /// <param name="potentialProxyInstance">The instance to unproxy.</param>
        /// <param name="targetInstance">The unproxied instance, if the unproxying succeeds.</param>
        /// <returns>`true` if the unproxying succeeds, `false` otherwise.</returns>
        bool TryUnproxy(object? potentialProxyInstance, [NotNullWhen(true)] out object? targetInstance);
    }
}
