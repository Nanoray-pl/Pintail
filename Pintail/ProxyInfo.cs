using System;
using System.Diagnostics.Contracts;

namespace Nanoray.Pintail
{
    public class ProxyInfo<C>: IEquatable<ProxyInfo<C>> where C : notnull, IEquatable<C>
    {
        public TypeInfo<C> Target { get; set; }
        public TypeInfo<C> Proxy { get; set; }

        public ProxyInfo(TypeInfo<C> target, TypeInfo<C> proxy)
        {
            Contract.Requires(proxy.Type.IsInterface);
            this.Target = target;
            this.Proxy = proxy;
        }

        public ProxyInfo<C> Copy(Type? targetType = null, Type? proxyType = null)
        {
            Contract.Requires(targetType == null || targetType.IsInterface);
            return new(
                target: new TypeInfo<C>(this.Target.Context, targetType ?? this.Target.Type),
                proxy: new TypeInfo<C>(this.Proxy.Context, proxyType ?? this.Proxy.Type)
            );
        }

        public bool Equals(ProxyInfo<C>? other)
            => other is not null && this.Target.Equals(other.Target) && this.Proxy.Equals(other.Proxy);

        public override bool Equals(object? obj)
            => obj is ProxyInfo<C> info && ((ProxyInfo<C>)this).Equals(info);

        public override int GetHashCode()
            => (this.Target, this.Proxy).GetHashCode();

        public static bool operator ==(ProxyInfo<C> left, ProxyInfo<C> right)
            => Equals(left, right);

        public static bool operator !=(ProxyInfo<C> left, ProxyInfo<C> right)
            => !Equals(left, right);
    }

    public class TypeInfo<C>: IEquatable<TypeInfo<C>> where C: notnull, IEquatable<C>
    {
        public C Context { get; set; }
        public Type Type { get; set; }

        public TypeInfo(C context, Type type)
        {
            this.Context = context;
            this.Type = type;
        }

        public bool Equals(TypeInfo<C>? other)
            => other is not null && this.Context.Equals(other.Context) && this.Type == other.Type;

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
