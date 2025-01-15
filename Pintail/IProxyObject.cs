namespace Nanoray.Pintail
{
    /// <summary>
    /// A marker interface for all proxy instances.
    /// </summary>
    public interface IProxyObject
    {
        /// <summary>
        /// A marker interface for all proxy instances, which also allows you to easily retrieve the underlying proxied instance.
        /// </summary>
        public interface IWithProxyTargetInstanceProperty: IProxyObject
        {
            /// <summary>
            /// The proxied instance.
            /// </summary>
            object ProxyTargetInstance { get; }
        }

        /// <summary>
        /// A marker interface for all proxy instances, which also allows you to retrieve the <see cref="ProxyInfo{T}"/> used for the proxy.
        /// </summary>
        public interface IWithProxyInfoProperty<Context>: IProxyObject
        {
            /// <summary>
            /// The proxy information describing this specific proxy object.
            /// </summary>
            ProxyInfo<Context> ProxyInfo { get; }
        }
    }
}
