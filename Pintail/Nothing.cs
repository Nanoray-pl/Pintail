namespace Nanoray.Pintail
{
    /// <summary>
    /// A type to be used in generic declarations, when no specific type is required.
    /// </summary>
    public struct Nothing
    {
        /// <summary>
        /// The only possible value of the <see cref="Nothing"/> type.
        /// </summary>
        public static Nothing AtAll => new();

        public override string ToString()
            => "[]";
    }
}
