using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace Nanoray.Pintail
{
    public enum UserConversionProxyProviderConversionType
    {
        None = 0,
        Implicit = 1 << 0,
        Explicit = 1 << 1,
        Any = Implicit | Explicit
    }

    public sealed class UserConversionProxyProvider : IProxyProvider
    {
        private enum LookupConversionType
        {
            Implicit, Explicit
        }

        public static double DefaultPriority { get; private set; } = 0.75;

        public UserConversionProxyProviderConversionType ConversionType { get; private init; }
        public double Priority { get; private init; }

        private Dictionary<(Type, Type), Delegate?> ConversionFunctions { get; } = new();

        public UserConversionProxyProvider(UserConversionProxyProviderConversionType conversionType, double? priority = null)
        {
            this.ConversionType = conversionType;
            this.Priority = priority is null ? DefaultPriority : priority.Value;
        }

        bool IProxyProvider.CanProxy<TOriginal, TProxy>(TOriginal original, [NotNullWhen(true)] out IProxyProcessor<TOriginal, TProxy>? processor, IProxyProvider? rootProvider)
        {
            Func<TOriginal, TProxy>? conversionFunction = null;

            lock (this.ConversionFunctions)
            {
                if (!this.ConversionFunctions.TryGetValue((typeof(TOriginal), typeof(TProxy)), out var @delegate))
                {
                    if (((int)this.ConversionType & (int)UserConversionProxyProviderConversionType.Implicit) != 0)
                        @delegate = this.MakeConversionFunction<TOriginal, TProxy>(LookupConversionType.Implicit);
                    if (@delegate is null && ((int)this.ConversionType & (int)UserConversionProxyProviderConversionType.Explicit) != 0)
                        @delegate = this.MakeConversionFunction<TOriginal, TProxy>(LookupConversionType.Explicit);
                    this.ConversionFunctions[(typeof(TOriginal), typeof(TProxy))] = @delegate;
                }

                if (@delegate is not null)
                    conversionFunction = (Func<TOriginal, TProxy>)@delegate;
            }

            if (conversionFunction is null)
            {
                processor = null;
                return false;
            }

            processor = new DelegateProxyProcessor<TOriginal, TProxy>(this.Priority, original, conversionFunction);
            return true;
        }

        private Func<TOriginal, TProxy>? MakeConversionFunction<TOriginal, TProxy>(LookupConversionType conversionType)
        {
            string methodName = conversionType switch
            {
                LookupConversionType.Implicit => "op_Implicit",
                LookupConversionType.Explicit => "op_Explicit",
                _ => throw new ArgumentException($"{nameof(LookupConversionType)} has an invalid value.")
            };

            MethodInfo? FindConversionMethod(Type type)
            {
                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(TOriginal) }, null);
                if (method is null)
                    return null;
                if (method.ReturnType != typeof(TProxy))
                    return null;
                return method;
            }

            var conversionMethod = FindConversionMethod(typeof(TOriginal)) ?? FindConversionMethod(typeof(TProxy));
            if (conversionMethod is null)
                return null;

            var method = new DynamicMethod("Convert", typeof(TProxy), new Type[] { typeof(TOriginal) });
            var il = method.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0); // load `original`
            il.Emit(OpCodes.Call, conversionMethod);
            il.Emit(OpCodes.Ret);

            return method.CreateDelegate<Func<TOriginal, TProxy>>();
        }
    }
}