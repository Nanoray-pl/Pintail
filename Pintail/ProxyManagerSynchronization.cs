namespace Nanoray.Pintail
{
    /// <summary>
    /// Defines how a proxy manager synchronizes access to it.
    /// </summary>
    public enum ProxyManagerSynchronization
    {
        /// <summary>
        /// No synchronization will take place.
        /// </summary>
        None,

        /// <summary>
        /// The proxy manager will synchronize access to itself via a lock.
        /// </summary>
        ViaLock
    }
}
