using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace Nanoray.Pintail
{
    public sealed class UserConversionProxyProvider : IProxyProvider
    {
        private enum LookupConversionType
        {
            Implicit, Explicit
        }

        public static double DefaultImplicitPriority { get; private set; } = 0.85;
        public static double DefaultExplicitPriority { get; private set; } = 0.75;

        public double ImplicitPriority { get; private init; }
        public double ExplicitPriority { get; private init; }

        private Dictionary<(Type, Type), (Delegate? Delegate, double Priority)> ConversionFunctions { get; } = new();

        public UserConversionProxyProvider(double? implicitPriority = null, double? explicitPriority = null)
        {
            this.ImplicitPriority = implicitPriority is null ? DefaultImplicitPriority : implicitPriority.Value;
            this.ExplicitPriority = explicitPriority is null ? DefaultExplicitPriority : explicitPriority.Value;
        }

        bool IProxyProvider.CanProxy<TOriginal, TProxy>(TOriginal original, [NotNullWhen(true)] out IProxyProcessor<TOriginal, TProxy>? processor, IProxyProvider? rootProvider)
        {
            (Func<TOriginal, TProxy> Function, double Priority)? conversionFunction = null;

            lock (this.ConversionFunctions)
            {
                if (!this.ConversionFunctions.TryGetValue((typeof(TOriginal), typeof(TProxy)), out var @delegate))
                {
                    if (this.ImplicitPriority > 0)
                        @delegate = (this.MakeConversionFunction<TOriginal, TProxy>(LookupConversionType.Implicit), this.ImplicitPriority);
                    if (@delegate.Delegate is null && this.ExplicitPriority > 0)
                        @delegate = (this.MakeConversionFunction<TOriginal, TProxy>(LookupConversionType.Explicit), this.ExplicitPriority);
                    this.ConversionFunctions[(typeof(TOriginal), typeof(TProxy))] = @delegate;
                }

                if (@delegate.Delegate is not null)
                    conversionFunction = ((Func<TOriginal, TProxy>)@delegate.Delegate, @delegate.Priority);
            }

            if (conversionFunction is null)
            {
                processor = null;
                return false;
            }

            processor = new DelegateProxyProcessor<TOriginal, TProxy>(conversionFunction.Value.Priority, original, conversionFunction.Value.Function);
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