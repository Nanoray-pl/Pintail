namespace Nanoray.Pintail
{
    /// <summary>
    /// Defines whether a proxy type should implement any marker interfaces.<br/>
    /// See also: <seealso cref="IProxyObject"/>, <seealso cref="IProxyObject.IWithProxyTargetInstanceProperty"/>.
    /// </summary>
    public enum ProxyObjectInterfaceMarking
    {
        /// <summary>
        /// Do not implement any marker interfaces.
        /// </summary>
        Disabled,

        /// <summary>
        /// Implement the <see cref="IProxyObject"/> interface.
        /// </summary>
        Marker,

        /// <summary>
        /// Implement the <see cref="IProxyObject.IWithProxyTargetInstanceProperty"/> interface.
        /// </summary>
        MarkerWithProperty
    }
}
