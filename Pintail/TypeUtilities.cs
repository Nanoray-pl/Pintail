using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;

namespace Nanoray.Pintail
{
    internal static class TypeUtilities
    {
        static readonly MemoryCache cache = new("ProxyCache");

        /// <summary>
        /// Controls how the target interface should compare to the proxy interface.
        /// </summary>
        internal enum MethodTypeAssignability
        {
            /// <summary>
            /// It is sufficient if the target typed can be assigned to the proxy type.
            /// </summary>
            AssignTo,

            /// <summary>
            /// It is sufficient if the target type can be assigned from the proxy type.
            /// </summary>
            AssignFrom,

            /// <summary>
            /// This type should exactly match the other type. Assigning to/from is not appropriate.
            /// </summary>
            Exact
        }

        internal enum MethodTypeMatchingPart{ ReturnType, Parameter }
        internal enum MatchingTypesResult { False, IfProxied, Assignable, Exact }
        // Assignable is not currently supported.
        internal enum PositionConversion { Proxy, Assignable, Exact }

        internal static MatchingTypesResult AreTypesMatching(Type targetType, Type proxyType, MethodTypeAssignability assignability, ProxyManagerEnumMappingBehavior enumMappingBehavior, ImmutableHashSet<Type> assumeMappableIfRecursed)
        {
            if (targetType.IsGenericMethodParameter != proxyType.IsGenericMethodParameter)
                return MatchingTypesResult.False;

            if (targetType.IsByRef != proxyType.IsByRef)
                return MatchingTypesResult.False;

            // Exact match, don't need to look further.
            if (proxyType == targetType)
                return MatchingTypesResult.Exact;

            // toss the by ref ness? Not sure here.
            if (targetType.IsByRef && proxyType.IsByRef)
            {
                targetType = targetType.GetNonRefType();
                proxyType = proxyType.GetNonRefType();
            }

            if (proxyType.IsEnum && targetType.IsEnum)
            {
                if (proxyType.IsGenericParameter && targetType.IsGenericParameter)
                    return MatchingTypesResult.IfProxied;

                // If the backing types don't match, don't try to proxy.
                if (proxyType.GetEnumUnderlyingType() != targetType.GetEnumUnderlyingType())
                    return MatchingTypesResult.False;

                var proxyEnumRawValues = proxyType.GetEnumerableEnumValues().Select(e => Convert.ChangeType(e, proxyType.GetEnumUnderlyingType())).ToHashSet();
                var targetEnumRawValues = targetType.GetEnumerableEnumValues().Select(e => Convert.ChangeType(e, proxyType.GetEnumUnderlyingType())).ToHashSet();
                switch (enumMappingBehavior)
                {
                    case ProxyManagerEnumMappingBehavior.Strict:
                        return proxyEnumRawValues == targetEnumRawValues ? MatchingTypesResult.IfProxied : MatchingTypesResult.False;
                    case ProxyManagerEnumMappingBehavior.AllowAdditive:
                        return proxyEnumRawValues.IsSubsetOf(targetEnumRawValues) ? MatchingTypesResult.IfProxied : MatchingTypesResult.False;
                    case ProxyManagerEnumMappingBehavior.ThrowAtRuntime:
                        return MatchingTypesResult.IfProxied;
                }
            }

            if (proxyType.IsArray && targetType.IsArray)
                return (MatchingTypesResult)Math.Min((int)AreTypesMatching(targetType.GetElementType()!, proxyType.GetElementType()!, assignability, enumMappingBehavior, assumeMappableIfRecursed), (int)MatchingTypesResult.IfProxied);

            // check mismatched generics.
            if (proxyType.IsGenericMethodParameter)
                return targetType.GenericParameterPosition == proxyType.GenericParameterPosition ? MatchingTypesResult.Exact : MatchingTypesResult.False;

            if (proxyType.IsAssignableTo(typeof(Delegate)) != targetType.IsAssignableTo(typeof(Delegate)))
                return MatchingTypesResult.False;

            // not convinced this works well for ref/out params????
            //if (typeA.IsAssignableFrom(typeB))
            //    return MatchingTypesResult.Assignable;

            // The boxing/unboxing bug probably isn't gone either.
            if (targetType.IsInterface)
            {
                if (assumeMappableIfRecursed.Contains(targetType))
                    return MatchingTypesResult.IfProxied; // we will need to double check this later.
                if (CanInterfaceBeMapped(targetType, proxyType, enumMappingBehavior, assignability, assumeMappableIfRecursed))
                    return MatchingTypesResult.IfProxied;
                return MatchingTypesResult.False;
            }
            if (proxyType.IsInterface)
            {
                if (assumeMappableIfRecursed.Contains(proxyType))
                    return MatchingTypesResult.IfProxied; // we will need to double check this later.
                if (CanInterfaceBeMapped(targetType, proxyType, enumMappingBehavior, assignability, assumeMappableIfRecursed))
                    return MatchingTypesResult.IfProxied;
                return MatchingTypesResult.False;
            }

            var targetTypeGenericArguments = targetType.GetGenericArguments();
            var proxyTypeGenericArguments = proxyType.GetGenericArguments();
            if (targetTypeGenericArguments.Length != proxyTypeGenericArguments.Length || targetTypeGenericArguments.Length == 0)
                return MatchingTypesResult.False;

            var matchingTypesResult = MatchingTypesResult.Exact;

            // I'm not convinced this ever gets run?
            if (!(proxyType.IsAssignableTo(typeof(Delegate)) && targetType.IsAssignableTo(typeof(Delegate))))
            {
                if (!targetType.IsGenericTypeDefinition && !proxyType.IsGenericTypeDefinition)
                {
                    var genericTargetType = targetType.GetGenericTypeDefinition();
                    var genericProxyType = proxyType.GetGenericTypeDefinition();
                    switch (AreTypesMatching(genericTargetType, genericProxyType, assignability, enumMappingBehavior, assumeMappableIfRecursed))
                    {
                        case MatchingTypesResult.Exact:
                        case MatchingTypesResult.Assignable:
                            break;
                        case MatchingTypesResult.IfProxied:
                            matchingTypesResult = (MatchingTypesResult)Math.Min((int)matchingTypesResult, (int)MatchingTypesResult.IfProxied);
                            break;
                        case MatchingTypesResult.False:
                            return MatchingTypesResult.False;
                    }
                }
            }
            for (int i = 0; i < targetTypeGenericArguments.Length; i++)
            {
                switch (AreTypesMatching(targetTypeGenericArguments[i], proxyTypeGenericArguments[i], assignability, enumMappingBehavior, assumeMappableIfRecursed))
                {
                    case MatchingTypesResult.Exact:
                    case MatchingTypesResult.Assignable:
                        break;
                    case MatchingTypesResult.IfProxied:
                        matchingTypesResult = (MatchingTypesResult)Math.Min((int)matchingTypesResult, (int)MatchingTypesResult.IfProxied);
                        break;
                    case MatchingTypesResult.False:
                        return MatchingTypesResult.False;
                }
            }
            return matchingTypesResult;
        }

        internal static PositionConversion?[]? MatchProxyMethod(MethodInfo targetMethod, MethodInfo proxyMethod, ProxyManagerEnumMappingBehavior enumMappingBehavior, ImmutableHashSet<Type> assumeMappableIfRecursed)
        {
            // checking if `targetMethod` matches `proxyMethod`
            var proxyMethodParameters = proxyMethod.GetParameters();
            var proxyMethodGenericArguments = proxyMethod.GetGenericArguments();

            if (targetMethod.Name != proxyMethod.Name)
                return null;
            if (targetMethod.GetGenericArguments().Length != proxyMethodGenericArguments!.Length)
                return null;
            var mParameters = targetMethod.GetParameters();
            if (mParameters.Length != proxyMethodParameters!.Length)
                return null;
            var positionConversions = new PositionConversion?[mParameters.Length + 1]; // 0 = return type; n + 1 = parameter position n

            switch (AreTypesMatching(targetMethod.ReturnType, proxyMethod.ReturnType, MethodTypeAssignability.AssignFrom, enumMappingBehavior, assumeMappableIfRecursed))
            {
                case MatchingTypesResult.False:
                    return null;
                case MatchingTypesResult.Exact:
                    break;
                case MatchingTypesResult.IfProxied:
                    positionConversions[0] = PositionConversion.Proxy;
                    break;
            }

            for (int i = 0; i < mParameters.Length; i++)
            {
                switch (AreTypesMatching(mParameters[i].ParameterType, proxyMethodParameters[i].ParameterType, MethodTypeAssignability.AssignTo, enumMappingBehavior, assumeMappableIfRecursed))
                {
                    case MatchingTypesResult.False:
                        return null;
                    case MatchingTypesResult.Exact:
                        break;
                    case MatchingTypesResult.IfProxied:
                        positionConversions[i + 1] = PositionConversion.Proxy;
                        break;
                }
            }

            // method matched
            return positionConversions;
        }

        // This recursion might be dangerous. I'm not sure.
        // Todo: figure out what else I need to do for recursion to avoid infinite loops.
        internal static bool CanInterfaceBeMapped(Type target, Type proxy, ProxyManagerEnumMappingBehavior enumMappingBehavior, MethodTypeAssignability assignability, ImmutableHashSet<Type> assumeMappableIfRecursed)
        {
            // If it's just assignable, we can skip the whole reflection logic
            // which can be quite slow.
            switch (assignability)
            {
                case MethodTypeAssignability.AssignTo:
                    if (target.IsAssignableTo(proxy))
                        return true;
                    break;
                case MethodTypeAssignability.AssignFrom:
                    if (target.IsAssignableFrom(proxy))
                        return true;
                    break;
                case MethodTypeAssignability.Exact:
                    if (target == proxy)
                        return true;
                    break;
            }

            // Rule out a few common issues with generics first.
            if (target.IsGenericType != proxy.IsGenericType)
                return false;

            if (target.IsGenericType && proxy.IsGenericType && target.GenericTypeArguments.Length != proxy.GenericTypeArguments.Length)
                return false;

            // check the cache.
            List<Type>? types = null;
            string cachekey = $"{target.AssemblyQualifiedName ?? $"{target.Assembly.GetName().Name}??{target.Namespace}??{target.Name}"}@@{enumMappingBehavior:D}@@{assignability:D}";
            if (cache.Contains(cachekey))
            {
                CacheItem? item = cache.GetCacheItem(cachekey);
                if (item.Value is List<Type>)
                {
                    types = (List<Type>)item.Value;
                    if (types.Contains(proxy))
                        return true;
                }
            }

            // Figure out groupby...
            var ToAssignToMethods = (assignability == MethodTypeAssignability.AssignTo ? target.FindInterfaceMethods() : proxy.FindInterfaceMethods());
            var ToAssignFromMethods = (assignability == MethodTypeAssignability.AssignTo ? proxy.FindInterfaceMethods() : target.FindInterfaceMethods());

            HashSet<MethodInfo> FoundMethods = new();
            assumeMappableIfRecursed = assumeMappableIfRecursed.Add(target).Add(proxy);

            foreach (var assignToMethod in ToAssignToMethods)
            {
                foreach (var assignFromMethod in ToAssignFromMethods)
                {
                    // The recursion here might be a problem.
                    // double check the directions are right here. Argh. I can never seem to get AssignTo/AssignFrom right on the first try.
                    if (TypeUtilities.MatchProxyMethod(assignToMethod, assignFromMethod, enumMappingBehavior, assumeMappableIfRecursed) is not null)
                    {
                        FoundMethods.Add(assignToMethod);
                        goto NextMethod;
                    }
                }
                return false;
NextMethod:
                ;
            }

            if (assignability == MethodTypeAssignability.Exact && FoundMethods != ToAssignFromMethods)
                return false;

            types ??= new();
            types.Add(proxy);

            cache.Add(new CacheItem(cachekey, types), new CacheItemPolicy() { SlidingExpiration = TimeSpan.FromMinutes(5)});
            return true;
        }

        internal static IEnumerable<MethodInfo> FindInterfaceMethods(this Type baseType)
        {
            foreach (MethodInfo method in baseType.GetMethods())
            {
                yield return method;
            }
            foreach (Type interfaceType in baseType.GetInterfaces())
            {
                foreach (var method in FindInterfaceMethods(interfaceType))
                {
                    yield return method;
                }
            }
        }
    }

    internal static class MethodTypeAssignabilityExtensions
    {
        internal static TypeUtilities.MethodTypeAssignability Swap(this TypeUtilities.MethodTypeAssignability assignability)
            => assignability switch
            {
                TypeUtilities.MethodTypeAssignability.AssignTo => TypeUtilities.MethodTypeAssignability.AssignFrom,
                TypeUtilities.MethodTypeAssignability.AssignFrom => TypeUtilities.MethodTypeAssignability.AssignTo,
                TypeUtilities.MethodTypeAssignability.Exact => TypeUtilities.MethodTypeAssignability.Exact,
                _ => throw new ArgumentException("Recieved unexpected enum value!"),
            };
    }
}
