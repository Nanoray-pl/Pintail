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
    }
}
