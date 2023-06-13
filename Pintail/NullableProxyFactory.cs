using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace Nanoray.Pintail
{
    internal class NullableProxyFactory<Context> : IProxyFactory<Context>
    {
        public ProxyInfo<Context> ProxyInfo { get; private set; }
        private readonly Dictionary<(Type, Type), Func<NullableProxyFactory<Context>, IProxyManager<Context>, object, object>> MapDelegateCache = new();

        internal NullableProxyFactory(ProxyInfo<Context> proxyInfo)
        {
            if (!proxyInfo.Target.Type.IsConstructedGenericType || proxyInfo.Target.Type.GetGenericTypeDefinition() != typeof(Nullable<>))
                throw new ArgumentException($"{proxyInfo.Target.Type.GetShortName()} is not a nullable type.");
            if (!proxyInfo.Proxy.Type.IsConstructedGenericType || proxyInfo.Proxy.Type.GetGenericTypeDefinition() != typeof(Nullable<>))
                throw new ArgumentException($"{proxyInfo.Proxy.Type.GetShortName()} is not a nullable type.");
            this.ProxyInfo = proxyInfo;
        }

        public object ObtainProxy(IProxyManager<Context> manager, object targetInstance)
        {
            var type = targetInstance.GetType();
            var @delegate = this.ObtainMapDelegate(manager, this.ProxyInfo.Target.Type.GetGenericArguments()[0], this.ProxyInfo.Proxy.Type.GetGenericArguments()[0]);
            return @delegate(this, manager, targetInstance);
        }

        public bool TryUnproxy(IProxyManager<Context> manager, object potentialProxyInstance, [NotNullWhen(true)] out object? targetInstance)
        {
            var type = potentialProxyInstance.GetType();
            var @delegate = this.ObtainMapDelegate(manager, this.ProxyInfo.Proxy.Type.GetGenericArguments()[0], this.ProxyInfo.Target.Type.GetGenericArguments()[0]);
            targetInstance = @delegate(this, manager, potentialProxyInstance);
            return true;
        }

        private Func<NullableProxyFactory<Context>, IProxyManager<Context>, object, object> ObtainMapDelegate(IProxyManager<Context> manager, Type inputType, Type outputType)
        {
            if (!this.MapDelegateCache.TryGetValue((inputType, outputType), out var @delegate))
            {
                @delegate = this.CreateMapDelegate(manager, inputType, outputType);
                this.MapDelegateCache[(inputType, outputType)] = @delegate;
            }
            return @delegate;
        }

        private Func<NullableProxyFactory<Context>, IProxyManager<Context>, object, object> CreateMapDelegate(IProxyManager<Context> manager, Type inputType, Type outputType)
        {
            var method = new DynamicMethod("Map", typeof(object), new Type[] { typeof(NullableProxyFactory<Context>), typeof(IProxyManager<Context>), typeof(object) });
            var il = method.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Unbox_Any, inputType);
            il.Emit(OpCodes.Newobj, typeof(Nullable<>).MakeGenericType(inputType).GetConstructor(new Type[] { inputType })!);
            il.Emit(OpCodes.Call, typeof(NullableProxyFactory<Context>).GetMethod(nameof(Map), BindingFlags.NonPublic | BindingFlags.Instance)!.MakeGenericMethod(new Type[] { inputType, outputType }));
            il.Emit(OpCodes.Box, typeof(Nullable<>).MakeGenericType(outputType));
            il.Emit(OpCodes.Ret);

            return method.CreateDelegate<Func<NullableProxyFactory<Context>, IProxyManager<Context>, object, object>>();
        }

        private R? Map<T, R>(IProxyManager<Context> manager, T? input)
            where T : struct
            where R : struct
        {
            if (input is null)
                return null;
            if (!manager.TryProxy<Context, R>(input.Value, this.ProxyInfo.Target.Context, this.ProxyInfo.Proxy.Context, out var result))
                throw new ArgumentException($"{input} cannot be mapped to type {typeof(R?).GetShortName()}.");
            return result;
        }
    }
}
