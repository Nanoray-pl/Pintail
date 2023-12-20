using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Nanoray.Pintail
{
    internal class ReconstructingProxyFactory<Context>: IProxyFactory<Context>
    {
        public ProxyInfo<Context> ProxyInfo { get; private set; }
        private readonly ProxyManagerEnumMappingBehavior EnumMappingBehavior;
        private readonly AccessLevelChecking AccessLevelChecking;
        private readonly ConcurrentDictionary<string, List<Type>> InterfaceMappabilityCache;

        private Func<IProxyManager<Context>, object, object> ProxyFactory = null!;
        private Func<IProxyManager<Context>, object, object> UnproxyFactory = null!;

        internal ReconstructingProxyFactory(
            ProxyInfo<Context> proxyInfo,
            ProxyManagerEnumMappingBehavior enumMappingBehavior,
            AccessLevelChecking accessLevelChecking,
            ConcurrentDictionary<string, List<Type>> interfaceMappabilityCache
        )
        {
            this.ProxyInfo = proxyInfo;
            this.EnumMappingBehavior = enumMappingBehavior;
            this.AccessLevelChecking = accessLevelChecking;
            this.InterfaceMappabilityCache = interfaceMappabilityCache;
        }

        internal void Prepare()
        {
            Func<IProxyManager<Context>, object, object>? MakeFactory(bool isReverse)
            {
                TypeInfo<Context> from = isReverse ? this.ProxyInfo.Proxy : this.ProxyInfo.Target;
                TypeInfo<Context> to = isReverse ? this.ProxyInfo.Target : this.ProxyInfo.Proxy;

                bool deconstructViaExtensionMethod;
                MethodInfo? deconstructMethod;
                int realParameterCount;

                if (from.Type.IsAssignableTo(typeof(ITuple)))
                {
                    var genericTypes = from.Type.GenericTypeArguments;
                    if (from.Type.Name.StartsWith("Tuple`"))
                    {
                        deconstructViaExtensionMethod = true;
                        deconstructMethod = typeof(TupleExtensions)
                            .GetMethods()
                            .Where(m => m.Name == "Deconstruct")
                            .First(m => m.GetGenericArguments().Length == genericTypes.Length)
                            .MakeGenericMethod(genericTypes);
                    }
                    else if (from.Type.Name.StartsWith("ValueTuple`"))
                    {
                        deconstructViaExtensionMethod = true;
                        deconstructMethod = this.GetType()
                            .GetMethods()
                            .Where(m => m.Name == "DeconstructValueTuple")
                            .First(m => m.GetGenericArguments().Length == genericTypes.Length)
                            .MakeGenericMethod(genericTypes);
                    }
                    else
                    {
                        throw new ArgumentException($"Cannot handle a tuple of type {from.Type.GetShortName()}.");
                    }
                }
                else
                {
                    deconstructViaExtensionMethod = false;
                    deconstructMethod = from.Type.GetMethod("Deconstruct");
                }

                if (deconstructMethod is null || deconstructMethod.ReturnType != typeof(void))
                    return null;

                var deconstructParameters = deconstructMethod.GetParameters();
                if (deconstructViaExtensionMethod)
                {
                    realParameterCount = deconstructMethod.GetParameters().Length - 1;
                    if (deconstructParameters[0].IsOut)
                        return null;
                    if (!deconstructParameters.Skip(1).All(p => p.IsOut))
                        return null;
                }
                else
                {
                    realParameterCount = deconstructMethod.GetParameters().Length;
                    if (!deconstructParameters.All(p => p.IsOut))
                        return null;
                }

                ParameterInfo[] factoryParameters;
                Func<object?[], object> factoryDelegate;

                if (to.Type.IsAssignableTo(typeof(ITuple)) && to.Type.Name.StartsWith("Tuple`"))
                {
                    var createMethod = typeof(Tuple)
                        .GetMethods()
                        .Where(m => m.Name == "Create")
                        .First(m => m.GetGenericArguments().Length == to.Type.GenericTypeArguments.Length)
                        .MakeGenericMethod(to.Type.GenericTypeArguments);
                    if (createMethod is null)
                        return null;

                    factoryParameters = createMethod.GetParameters();
                    factoryDelegate = parameters => createMethod.Invoke(null, parameters)!;
                }
                else
                {
                    var constructor = to.Type.GetConstructors().Where(c => c.GetParameters().Length == realParameterCount).FirstOrDefault();
                    if (constructor is null)
                        return null;

                    factoryParameters = constructor.GetParameters();
                    factoryDelegate = parameters => constructor.Invoke(parameters)!;
                }

                return (manager, from) =>
                {
                    object?[] deconstructValues = new object?[deconstructParameters.Length];
                    object?[] constructValues = new object?[realParameterCount];

                    if (deconstructViaExtensionMethod)
                    {
                        deconstructValues[0] = from;
                        deconstructMethod.Invoke(null, deconstructValues);
                    }
                    else
                    {
                        deconstructMethod.Invoke(from, deconstructValues);
                    }

                    for (int i = 0; i < constructValues.Length; i++)
                    {
                        int deconstructValueIndex = i + (deconstructViaExtensionMethod ? 1 : 0);
                        constructValues[i] = deconstructValues[deconstructValueIndex];
                        switch (TypeUtilities.AreTypesMatching(factoryParameters[i].ParameterType, deconstructValues[deconstructValueIndex]!.GetType(), TypeUtilities.MethodTypeAssignability.AssignTo, this.EnumMappingBehavior, ImmutableHashSet<Type>.Empty, this.InterfaceMappabilityCache, this.AccessLevelChecking == AccessLevelChecking.Disabled))
                        {
                            case TypeUtilities.MatchingTypesResult.Exact:
                            case TypeUtilities.MatchingTypesResult.Assignable:
                                break;
                            case TypeUtilities.MatchingTypesResult.IfProxied:
                                ProxyInfo<Context> proxyInfo = new(
                                    target: new TypeInfo<Context>(this.ProxyInfo.Target.Context, constructValues[i]!.GetType()),
                                    proxy: new TypeInfo<Context>(this.ProxyInfo.Proxy.Context, factoryParameters[i].ParameterType)
                                );
                                var factory = manager.ObtainProxyFactory(proxyInfo);
                                constructValues[i] = factory.ObtainProxy(manager, constructValues[i]!);
                                break;
                            case TypeUtilities.MatchingTypesResult.False:
                                throw new InvalidOperationException($"Cannot convert from {from} to type {to.Type}.");
                        }
                    }
                    return factoryDelegate(constructValues);
                };
            }

            this.ProxyFactory = MakeFactory(false) ?? throw new ArgumentException("Could not create mapping factory.");
            this.UnproxyFactory = MakeFactory(true) ?? throw new ArgumentException("Could not create mapping factory.");
        }

        public object ObtainProxy(IProxyManager<Context> manager, object targetInstance)
        {
            return this.ProxyFactory(manager, targetInstance);
        }

        public bool TryUnproxy(IProxyManager<Context> manager, object potentialProxyInstance, [NotNullWhen(true)] out object? targetInstance)
        {
            targetInstance = this.UnproxyFactory(manager, potentialProxyInstance);
            return true;
        }

        public static void DeconstructValueTuple<T1>(ValueTuple<T1> tuple, out T1 t1)
        {
            t1 = tuple.Item1;
        }

        public static void DeconstructValueTuple<T1, T2>(ValueTuple<T1, T2> tuple, out T1 t1, out T2 t2)
        {
            t1 = tuple.Item1;
            t2 = tuple.Item2;
        }

        public static void DeconstructValueTuple<T1, T2, T3>(ValueTuple<T1, T2, T3> tuple, out T1 t1, out T2 t2, out T3 t3)
        {
            t1 = tuple.Item1;
            t2 = tuple.Item2;
            t3 = tuple.Item3;
        }

        public static void DeconstructValueTuple<T1, T2, T3, T4>(ValueTuple<T1, T2, T3, T4> tuple, out T1 t1, out T2 t2, out T3 t3, out T4 t4)
        {
            t1 = tuple.Item1;
            t2 = tuple.Item2;
            t3 = tuple.Item3;
            t4 = tuple.Item4;
        }

        public static void DeconstructValueTuple<T1, T2, T3, T4, T5>(ValueTuple<T1, T2, T3, T4, T5> tuple, out T1 t1, out T2 t2, out T3 t3, out T4 t4, out T5 t5)
        {
            t1 = tuple.Item1;
            t2 = tuple.Item2;
            t3 = tuple.Item3;
            t4 = tuple.Item4;
            t5 = tuple.Item5;
        }

        public static void DeconstructValueTuple<T1, T2, T3, T4, T5, T6>(ValueTuple<T1, T2, T3, T4, T5, T6> tuple, out T1 t1, out T2 t2, out T3 t3, out T4 t4, out T5 t5, out T6 t6)
        {
            t1 = tuple.Item1;
            t2 = tuple.Item2;
            t3 = tuple.Item3;
            t4 = tuple.Item4;
            t5 = tuple.Item5;
            t6 = tuple.Item6;
        }

        public static void DeconstructValueTuple<T1, T2, T3, T4, T5, T6, T7>(ValueTuple<T1, T2, T3, T4, T5, T6, T7> tuple, out T1 t1, out T2 t2, out T3 t3, out T4 t4, out T5 t5, out T6 t6, out T7 t7)
        {
            t1 = tuple.Item1;
            t2 = tuple.Item2;
            t3 = tuple.Item3;
            t4 = tuple.Item4;
            t5 = tuple.Item5;
            t6 = tuple.Item6;
            t7 = tuple.Item7;
        }
    }
}
