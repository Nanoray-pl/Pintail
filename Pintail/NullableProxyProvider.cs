using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Nanoray.Pintail
{
    public class NullableProxyProvider : IProxyProvider
    {
        private Dictionary<(Type, Type), Delegate> CanProxyFunctions { get; } = new();
        private Dictionary<(Type, Type), Delegate> ObtainProxyFunctions { get; } = new();

        bool IProxyProvider.CanProxy<TOriginal, TProxy>(TOriginal original, IProxyProvider? rootProvider)
        {
            if (!typeof(TOriginal).IsGenericType || !typeof(TProxy).IsGenericType)
                return false;
            if (typeof(TOriginal).IsGenericTypeDefinition || typeof(TProxy).IsGenericTypeDefinition)
                return false;
            if (typeof(TOriginal).GetGenericTypeDefinition() != typeof(Nullable<>) || typeof(TProxy).GetGenericTypeDefinition() != typeof(Nullable<>))
                return false;

            var originalValueType = typeof(TOriginal).GenericTypeArguments[0];
            var proxyValueType = typeof(TProxy).GenericTypeArguments[0];
            var canProxyFunction = (Func<TOriginal, IProxyProvider?, bool>)ObtainCanProxyFunction(originalValueType, proxyValueType);
            return canProxyFunction(original, rootProvider);
        }

        TProxy IProxyProvider.ObtainProxy<TOriginal, TProxy>(TOriginal original, IProxyProvider? rootProvider)
        {
            var originalValueType = typeof(TOriginal).GenericTypeArguments[0];
            var proxyValueType = typeof(TProxy).GenericTypeArguments[0];
            var obtainProxyFunction = (Func<TOriginal, IProxyProvider?, TProxy>)ObtainObtainProxyFunction(originalValueType, proxyValueType);
            return obtainProxyFunction(original, rootProvider);
        }

        private static bool CanProxy<TOriginal, TProxy>(TOriginal? original, IProxyProvider? rootProvider = null)
            where TOriginal : struct
            where TProxy : struct
        {
            if (original.HasValue)
                return rootProvider?.CanProxy<TOriginal, TProxy>(original.Value, rootProvider) ?? false;
            else
                return true;
        }

        private static TProxy? ObtainProxy<TOriginal, TProxy>(TOriginal? original, IProxyProvider? rootProvider = null)
            where TOriginal : struct
            where TProxy : struct
        {
            if (original.HasValue)
                return rootProvider?.ObtainProxy<TOriginal, TProxy>(original.Value, rootProvider)
                    ?? throw new ArgumentException($"{typeof(NullableProxyProvider).Name} cannot proxy values on its own without a `{nameof(rootProvider)}`.");
            else
                return null;
        }

        private Delegate ObtainCanProxyFunction(Type originalValueType, Type proxyValueType)
        {
            lock (CanProxyFunctions)
            {
                if (!CanProxyFunctions.TryGetValue((originalValueType, proxyValueType), out var @delegate))
                {
                    @delegate = MakeCanProxyFunction(originalValueType, proxyValueType);
                    CanProxyFunctions[(originalValueType, proxyValueType)] = @delegate;
                }
                return @delegate;
            }
        }

        private Delegate ObtainObtainProxyFunction(Type originalValueType, Type proxyValueType)
        {
            lock (ObtainProxyFunctions)
            {
                if (!ObtainProxyFunctions.TryGetValue((originalValueType, proxyValueType), out var @delegate))
                {
                    @delegate = MakeObtainProxyFunction(originalValueType, proxyValueType);
                    ObtainProxyFunctions[(originalValueType, proxyValueType)] = @delegate;
                }
                return @delegate;
            }
        }

        private Delegate MakeCanProxyFunction(Type originalValueType, Type proxyValueType)
        {
            var selfType = GetType();
            var originalNullableType = typeof(Nullable<>).MakeGenericType(originalValueType);
            var proxyNullableType = typeof(Nullable<>).MakeGenericType(proxyValueType);
            var genericProxyProviderType = typeof(IProxyProvider);

            var originalNullableParameter = Expression.Parameter(originalNullableType, "original");
            var rootProviderParameter = Expression.Parameter(genericProxyProviderType, "rootProvider");

            var canProxyRawMethod = selfType.GetMethod(nameof(CanProxy), BindingFlags.NonPublic | BindingFlags.Static)!;
            var canProxyTypedMethod = canProxyRawMethod.MakeGenericMethod(originalValueType, proxyValueType);

            return Expression.Lambda(
                Expression.Call(
                    canProxyTypedMethod,
                    originalNullableParameter,
                    rootProviderParameter
                ),
                originalNullableParameter,
                rootProviderParameter
            ).Compile();
        }

        private Delegate MakeObtainProxyFunction(Type originalValueType, Type proxyValueType)
        {
            var selfType = GetType();
            var originalNullableType = typeof(Nullable<>).MakeGenericType(originalValueType);
            var proxyNullableType = typeof(Nullable<>).MakeGenericType(proxyValueType);
            var genericProxyProviderType = typeof(IProxyProvider);

            var originalNullableParameter = Expression.Parameter(originalNullableType, "original");
            var rootProviderParameter = Expression.Parameter(genericProxyProviderType, "rootProvider");

            var obtainProxyRawMethod = selfType.GetMethod(nameof(ObtainProxy), BindingFlags.NonPublic | BindingFlags.Static)!;
            var obtainProxyTypedMethod = obtainProxyRawMethod.MakeGenericMethod(originalValueType, proxyValueType);

            return Expression.Lambda(
                Expression.Call(
                    obtainProxyTypedMethod,
                    originalNullableParameter,
                    rootProviderParameter
                ),
                originalNullableParameter,
                rootProviderParameter
            ).Compile();
        }
    }
}