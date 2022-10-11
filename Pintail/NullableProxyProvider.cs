using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Nanoray.Pintail
{
    public class NullableProxyProvider : IProxyProvider
    {
        private delegate bool CanProxyDelegate<TOriginal, TProxy>(NullableProxyProvider self, TOriginal original, [NotNullWhen(true)] out IProxyProcessor<TOriginal, TProxy>? processor, IProxyProvider? rootProvider);

        public static double DefaultPriority { get; private set; } = 0.8;

        public double Priority { get; private init; }

        private Dictionary<(Type, Type), Delegate> CanProxyFunctions { get; } = new();

        public NullableProxyProvider(double? priority = null)
        {
            this.Priority = priority is null ? DefaultPriority : priority.Value;
        }

        bool IProxyProvider.CanProxy<TOriginal, TProxy>(TOriginal original, [NotNullWhen(true)] out IProxyProcessor<TOriginal, TProxy>? processor, IProxyProvider? rootProvider)
        {
            processor = null;
            if (!typeof(TOriginal).IsGenericType || !typeof(TProxy).IsGenericType)
                return false;
            if (typeof(TOriginal).IsGenericTypeDefinition || typeof(TProxy).IsGenericTypeDefinition)
                return false;
            if (typeof(TOriginal).GetGenericTypeDefinition() != typeof(Nullable<>) || typeof(TProxy).GetGenericTypeDefinition() != typeof(Nullable<>))
                return false;

            var originalValueType = typeof(TOriginal).GenericTypeArguments[0];
            var proxyValueType = typeof(TProxy).GenericTypeArguments[0];

            var canProxyFunction = (CanProxyDelegate<TOriginal, TProxy>)ObtainCanProxyFunction(originalValueType, proxyValueType);
            return canProxyFunction(this, original, out processor, rootProvider);
        }

        private bool CanProxy<TOriginal, TProxy>(TOriginal? original, [NotNullWhen(true)] out IProxyProcessor<TOriginal?, TProxy?>? processor, IProxyProvider? rootProvider)
            where TOriginal : struct
            where TProxy : struct
        {
            if (original is null)
            {
                processor = new DelegateProxyProcessor<TOriginal?, TProxy?>(Priority, original, _ => null);
                return true;
            }
            if (rootProvider is null)
            {
                processor = null;
                return false;
            }

            var canProxyValueResult = rootProvider.CanProxy<TOriginal, TProxy>(original.Value, out var valueProcessor, rootProvider);
            processor = canProxyValueResult && valueProcessor is not null ? new DelegateProxyProcessor<TOriginal?, TProxy?>(Priority, original, _ => valueProcessor.ObtainProxy()) : null;
            return canProxyValueResult;
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

        private Delegate MakeCanProxyFunction(Type originalValueType, Type proxyValueType)
        {
            var selfType = GetType();
            var originalNullableType = typeof(Nullable<>).MakeGenericType(originalValueType);
            var proxyNullableType = typeof(Nullable<>).MakeGenericType(proxyValueType);
            var processorRawType = typeof(IProxyProcessor<int, int>).GetGenericTypeDefinition();
            var nullableProcessorType = processorRawType.MakeGenericType(originalNullableType, proxyNullableType);
            var proxyProviderType = typeof(IProxyProvider);
            var delegateRawType = typeof(CanProxyDelegate<int, int>).GetGenericTypeDefinition();
            var delegateType = delegateRawType.MakeGenericType(originalNullableType, proxyNullableType);

            var canProxyRawMethod = selfType.GetMethod(nameof(CanProxy), BindingFlags.NonPublic | BindingFlags.Instance)!;
            var canProxyTypedMethod = canProxyRawMethod.MakeGenericMethod(originalValueType, proxyValueType);

            var method = new DynamicMethod("CanProxy", typeof(bool), new Type[] { selfType, originalNullableType, nullableProcessorType.MakeByRefType(), proxyProviderType });
            var il = method.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0); // load `this`
            il.Emit(OpCodes.Ldarg_1); // load `original`
            il.Emit(OpCodes.Ldarg_2); // load `processor`
            il.Emit(OpCodes.Ldarg_3); // load `rootProvider`
            il.Emit(OpCodes.Call, canProxyTypedMethod);
            il.Emit(OpCodes.Ret);

            return method.CreateDelegate(delegateType);
        }
    }
}