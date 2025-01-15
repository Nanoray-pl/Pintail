using System;

namespace Nanoray.Pintail
{
    /// <summary>
    /// Defines whether a proxy type should implement any marker interfaces.
    /// </summary>
    /// <seealso cref="IProxyObject"/>
    /// <seealso cref="IProxyObject.IWithProxyTargetInstanceProperty"/>
    [Flags]
    public enum ProxyObjectInterfaceMarking
    {
        /// <summary>
        /// Do not implement any marker interfaces.
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// Implement the <see cref="IProxyObject"/> interface.
        /// </summary>
        Marker = 1 << 0,

        /// <summary>
        /// Implement the <see cref="IProxyObject.IWithProxyTargetInstanceProperty"/> interface.
        /// </summary>
        [Obsolete("Use the `IncludeProxyTargetInstance` flag instead.")]
        MarkerWithProperty = 1 << 1,

        /// <summary>
        /// Implement the <see cref="IProxyObject.IWithProxyTargetInstanceProperty"/> interface.
        /// </summary>
        /// <remarks>
        /// Including this flag implies the <see cref="Marker"/> flag too.
        /// </remarks>
        IncludeProxyTargetInstance = 1 << 1,

        /// <summary>
        /// Implement the <see cref="IProxyObject.IWithProxyInfoProperty{Context}"/> interface.
        /// </summary>
        /// <remarks>
        /// Including this flag implies the <see cref="Marker"/> flag too.
        /// </remarks>
        IncludeProxyInfo = 1 << 2
    }
}
