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
        /// Creates a new <see cref="ProxyInfo{}"/>.
        /// </summary>
        /// <param name="target">The context of the target instance.</param>
        /// <param name="proxy">The context of the proxy instance.</param>
        public ProxyInfo(TypeInfo<Context> target, TypeInfo<Context> proxy)
        {
            if (!proxy.Type.IsInterface)
                throw new ArgumentException($"{proxy.Type.GetBestName()} has to be an interface.");
            this.Target = target;
            this.Proxy = proxy;
        }

        /// <summary>
        /// Creates a copy of this <see cref="ProxyInfo{}"/> with a different set of target and/or proxy types.
        /// </summary>
        /// <param name="targetType">The new target type.</param>
        /// <param name="proxyType">The new proxy type.</param>
        /// <returns>A copy with specified properties.</returns>
        public ProxyInfo<Context> Copy(Type? targetType = null, Type? proxyType = null)
        {
            if (proxyType is not null && !proxyType.IsInterface)
                throw new ArgumentException($"{proxyType.GetBestName()} has to be an interface.");
            return new(
                target: new TypeInfo<Context>(this.Target.Context, targetType ?? this.Target.Type),
                proxy: new TypeInfo<Context>(this.Proxy.Context, proxyType ?? this.Proxy.Type)
            );
        }

        public bool Equals(ProxyInfo<Context>? other)
            => other is not null && this.Target.Equals(other.Target) && this.Proxy.Equals(other.Proxy);

        public override bool Equals(object? obj)
            => obj is ProxyInfo<Context> info && ((ProxyInfo<Context>)this).Equals(info);

        public override int GetHashCode()
            => (this.Target, this.Proxy).GetHashCode();

        public static bool operator ==(ProxyInfo<Context> left, ProxyInfo<Context> right)
            => Equals(left, right);

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
        /// Creates a new <see cref="TypeInfo{}"/>.
        /// </summary>
        /// <param name="context">The context type used to describe the current proxy process.</param>
        /// <param name="type">The type to proxy from/to.</param>
        public TypeInfo(C context, Type type)
        {
            this.Context = context;
            this.Type = type;
        }

        public bool Equals(TypeInfo<C>? other)
            => other is not null && (typeof(C).GetInterfacesRecursively(true).Contains(typeof(IEquatable<C>)) ? ((IEquatable<C>)other).Equals(this) : (Equals(this.Context, other.Context) && Equals(this.Type, other.Type)));

        public override bool Equals(object? obj)
            => obj is TypeInfo<C> info && ((TypeInfo<C>)this).Equals(info);

        public override int GetHashCode()
            => (this.Context, this.Type).GetHashCode();

        public static bool operator ==(TypeInfo<C>? left, TypeInfo<C>? right)
            => Equals(left, right);

        public static bool operator !=(TypeInfo<C>? left, TypeInfo<C>? right)
            => !Equals(left, right);
    }
}
