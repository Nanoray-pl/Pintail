using System;

namespace Nanoray.Pintail
{
    /// <summary>
    /// A type to be used in generic declarations, when no specific type is required.
    /// </summary>
    public readonly struct Nothing : IEquatable<Nothing>
    {
        /// <summary>
        /// The only possible value of the <see cref="Nothing"/> type.
        /// </summary>
        public static Nothing AtAll { get; } = new();

        /// <inheritdoc/>
        public override string ToString()
            => "{}";

        /// <inheritdoc/>
        public bool Equals(Nothing other)
            => true;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is Nothing;

        /// <inheritdoc/>
        public override int GetHashCode()
            => 0;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static bool operator ==(Nothing left, Nothing right)
            => true;

        public static bool operator !=(Nothing left, Nothing right)
            => false;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
