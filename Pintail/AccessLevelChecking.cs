namespace Nanoray.Pintail
{
    /// <summary>
    /// Defines whether access level checks should be enabled for generated proxy types.
    /// </summary>
    public enum AccessLevelChecking
    {
        /// <summary>
        /// All access level checks are disabled.
        /// </summary>
        Disabled,

        /// <summary>
        /// All access level checks are disabled, but only public members will be proxied.
        /// </summary>
        DisabledButOnlyAllowPublicMembers,

        /// <summary>
        /// All access level checks are enabled as usual. This can lead to some exceptions being thrown, if the proxied type (or its encompassing type) is not public.
        /// </summary>
        Enabled
    }
}
