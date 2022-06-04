using System.Diagnostics.CodeAnalysis;

namespace Nanoray.Pintail
{
    /// <summary>
    /// Represents a type responsible for proxying/mapping a specific type back and forth.
    /// </summary>
    /// <typeparam name="Context">The context type used to describe the current proxy process. Use <see cref="Nothing"/> if not needed.</typeparam>
    public interface IProxyFactory<Context>
    {
        /// <summary>
        /// The proxy information describing this specific <see cref="IProxyFactory{Context}"/>.
        /// </summary>
        ProxyInfo<Context> ProxyInfo { get; }

        /// <summary>
        /// Returns a proxy instance for a given instance.
        /// </summary>
        /// <param name="manager">The <see cref="IProxyManager{Context}"/> managing this <see cref="IProxyFactory{Context}"/>.</param>
        /// <param name="targetInstance">The instance to create a proxy for.</param>
        /// <returns>A proxy of the given instance.</returns>
        object ObtainProxy(IProxyManager<Context> manager, object targetInstance);

        /// <summary>
        /// Tries to unproxy a given instance.
        /// </summary>
        /// <param name="manager">The <see cref="IProxyManager{Context}"/> managing this <see cref="IProxyFactory{Context}"/>.</param>
        /// <param name="potentialProxyInstance">The instance to unproxy.</param>
        /// <param name="targetInstance">The unproxied instance, if the unproxying succeeds.</param>
        /// <returns>`true` if the unproxying succeeds, `false` otherwise.</returns>
        bool TryUnproxy(IProxyManager<Context> manager, object potentialProxyInstance, [NotNullWhen(true)] out object? targetInstance);
    }
}
