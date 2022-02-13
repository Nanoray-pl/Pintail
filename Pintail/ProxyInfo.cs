using System;

namespace Nanoray.Pintail
{
    public class ProxyInfo<C>: IEquatable<ProxyInfo<C>>
    {
        public TypeInfo<C> Target { get; set; }
        public TypeInfo<C> Proxy { get; set; }

        public ProxyInfo(TypeInfo<C> target, TypeInfo<C> proxy)
        {
            if (!proxy.Type.IsInterface)
                throw new ArgumentException($"{proxy.Type.FullName} has to be an interface.");
            this.Target = target;
            this.Proxy = proxy;
        }

        public ProxyInfo<C> Copy(Type? targetType = null, Type? proxyType = null)
        {
            if (proxyType is not null && !proxyType.IsInterface)
                throw new ArgumentException($"{proxyType.FullName} has to be an interface.");
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

    public class TypeInfo<C>: IEquatable<TypeInfo<C>>
    {
        public C Context { get; set; }
        public Type Type { get; set; }

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
