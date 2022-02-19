using System;

namespace Nanoray.Pintail
{
    /// <summary>
    /// Describes the specific proxy conversion.
    /// </summary>
    /// <typeparam name="Context">The context type used to describe the current proxy process. Use <see cref="Nothing"/> if not needed.</typeparam>
    public class ProxyInfo<Context>: IEquatable<ProxyInfo<Context>>
    {
        /// <summary>
        /// The context of the target instance.
        /// </summary>
        public readonly TypeInfo<Context> Target;

        /// <summary>
        /// The context of the proxy instance.
        /// </summary>
        public readonly TypeInfo<Context> Proxy;

        /// <summary>
        /// Creates a new <see cref="ProxyInfo{Context}"/>.
        /// </summary>
        /// <param name="target">The context of the target instance.</param>
        /// <param name="proxy">The context of the proxy instance.</param>
        public ProxyInfo(TypeInfo<Context> target, TypeInfo<Context> proxy)
        {
            this.Target = target;
            this.Proxy = proxy;
        }

        /// <summary>
        /// Creates a copy of this <see cref="ProxyInfo{Context}"/> with a different set of target and/or proxy types.
        /// </summary>
        /// <param name="targetType">The new target type.</param>
        /// <param name="proxyType">The new proxy type.</param>
        /// <returns>A copy with specified properties.</returns>
        public ProxyInfo<Context> Copy(Type? targetType = null, Type? proxyType = null)
        {
            return new(
                target: new TypeInfo<Context>(this.Target.Context, targetType ?? this.Target.Type),
                proxy: new TypeInfo<Context>(this.Proxy.Context, proxyType ?? this.Proxy.Type)
            );
        }

        /// <summary>
        /// Creates a copy of this <see cref="ProxyInfo{Context}"/> that is a reverse of its target and proxy types.
        /// </summary>
        /// <returns>A copy with reversed target and proxy types.</returns>
        public ProxyInfo<Context> Reversed()
        {
            return this.Copy(targetType: this.Proxy.Type, proxyType: this.Target.Type);
        }

        /// <inheritdoc/>
        public override string ToString()
            => $"ProxyInfo{{target: {this.Target}, proxy: {this.Proxy}}}";

        /// <inheritdoc/>
        public bool Equals(ProxyInfo<Context>? other)
            => other is not null && this.Target.Equals(other.Target) && this.Proxy.Equals(other.Proxy);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is ProxyInfo<Context> info && this.Equals(info);

        /// <inheritdoc/>
        public override int GetHashCode()
            => (this.Target, this.Proxy).GetHashCode();

        /// <inheritdoc/>
        public static bool operator ==(ProxyInfo<Context> left, ProxyInfo<Context> right)
            => Equals(left, right);

        /// <inheritdoc/>
        public static bool operator !=(ProxyInfo<Context> left, ProxyInfo<Context> right)
            => !Equals(left, right);
    }

    /// <summary>
    /// Describes one side of a specified proxy conversion.
    /// </summary>
    /// <typeparam name="C">The context type used to describe the current proxy process. Use <see cref="Nothing"/> if not needed.</typeparam>
    public class TypeInfo<C>: IEquatable<TypeInfo<C>>
    {
        /// <summary>
        /// The context type used to describe the current proxy process.
        /// </summary>
        public readonly C Context;

        /// <summary>
        /// The type to proxy from/to.
        /// </summary>
        public readonly Type Type;

        /// <summary>
        /// Creates a new <see cref="TypeInfo{C}"/>.
        /// </summary>
        /// <param name="context">The context type used to describe the current proxy process.</param>
        /// <param name="type">The type to proxy from/to.</param>
        public TypeInfo(C context, Type type)
        {
            this.Context = context;
            this.Type = type;
        }

        /// <inheritdoc/>
        public override string ToString()
            => $"TypeInfo{{context: {this.Context}, type: {this.Type.GetBestName()}}}";

        /// <inheritdoc/>
        public bool Equals(TypeInfo<C>? other)
            => other is not null && other.Type == this.Type && Equals(other.Context, this.Context);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is TypeInfo<C> info && this.Equals(info);

        /// <inheritdoc/>
        public override int GetHashCode()
            => (this.Context, this.Type).GetHashCode();

        /// <inheritdoc/>
        public static bool operator ==(TypeInfo<C>? left, TypeInfo<C>? right)
            => Equals(left, right);

        /// <inheritdoc/>
        public static bool operator !=(TypeInfo<C>? left, TypeInfo<C>? right)
            => !Equals(left, right);
    }
}
