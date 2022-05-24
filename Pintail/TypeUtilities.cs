using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;

namespace Nanoray.Pintail
{
    internal static class TypeUtilities
    {
        static readonly MemoryCache cache = new("ProxyCache");

        /// <summary>
        /// Documentation is hard.
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

        internal static MatchingTypesResult AreTypesMatching(Type targetType, Type proxyType, MethodTypeMatchingPart part, ProxyManagerEnumMappingBehavior enumMappingBehavior)
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

            // I feel like ref ness will cause issues here.
            var typeA = part == MethodTypeMatchingPart.Parameter ? targetType : proxyType;
            var typeB = part == MethodTypeMatchingPart.Parameter ? proxyType : targetType;

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
                        return targetEnumRawValues.IsSubsetOf(proxyEnumRawValues) ? MatchingTypesResult.False : MatchingTypesResult.IfProxied;
                    case ProxyManagerEnumMappingBehavior.ThrowAtRuntime:
                        return MatchingTypesResult.IfProxied;
                }
            }

            if (proxyType.IsArray && targetType.IsArray)
                return (MatchingTypesResult)Math.Min((int)AreTypesMatching(targetType.GetElementType()!, proxyType.GetElementType()!, part, enumMappingBehavior), (int)MatchingTypesResult.IfProxied);

            if (typeA.IsGenericMethodParameter)
                return typeA.GenericParameterPosition == typeB.GenericParameterPosition ? MatchingTypesResult.Exact : MatchingTypesResult.False;

            if (proxyType.IsAssignableTo(typeof(Delegate)) != targetType.IsAssignableTo(typeof(Delegate)))
                return MatchingTypesResult.False;

            // not convinced this works well for ref/out params????
            //if (typeA.IsAssignableFrom(typeB))
            //    return MatchingTypesResult.Assignable;

            // The boxing/unboxing bug probably isn't gone either.
            if (typeA.IsInterface || typeB.IsInterface)
            { // I feel like more checks are needed here? Like, uh, the **type** of the property....This is probably bad.
                if (typeA.GetProperties().Select((a) => a.Name).ToHashSet().IsSubsetOf(typeB.GetProperties().Select((b) => b.Name)))
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
                    switch (AreTypesMatching(genericTargetType, genericProxyType, part, enumMappingBehavior))
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
                switch (AreTypesMatching(targetTypeGenericArguments[i], proxyTypeGenericArguments[i], part, enumMappingBehavior))
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

        internal static PositionConversion?[]? MatchProxyMethod(MethodInfo targetMethod, MethodInfo proxyMethod, ProxyManagerEnumMappingBehavior enumMappingBehavior)
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

            switch (AreTypesMatching(targetMethod.ReturnType, proxyMethod.ReturnType, MethodTypeMatchingPart.ReturnType, enumMappingBehavior))
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
                switch (AreTypesMatching(mParameters[i].ParameterType, proxyMethodParameters[i].ParameterType, MethodTypeMatchingPart.Parameter, enumMappingBehavior))
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



        internal static bool CanInterfaceBeMapped(Type target, Type proxy, ProxyManagerEnumMappingBehavior enumMappingBehavior, MethodTypeAssignability assignability)
        {
            List<Type>? types = null;
            string cachekey = $"{target.FullName}@@{enumMappingBehavior:D}@@{assignability:D}";
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

            

            HashSet<MethodInfo> ToAssignToMethods = (assignability == MethodTypeAssignability.AssignTo ? target.FindInterfaceMethods() : proxy.FindInterfaceMethods()).ToHashSet();
            HashSet<MethodInfo> ToAssignFromMethods = (assignability == MethodTypeAssignability.AssignTo? proxy.FindInterfaceMethods() : target.FindInterfaceMethods()).ToHashSet();

            HashSet<MethodInfo> FoundMethods = new();

            foreach (var assignToMethod in ToAssignToMethods)
            {
                foreach (var assignFromMethod in ToAssignFromMethods)
                {
                    // double check the directions are right here. Argh. I can never seem to get AssignTo/AssignFrom right on the first try.
                    if (TypeUtilities.MatchProxyMethod(assignToMethod, assignFromMethod, enumMappingBehavior) is not null)
                    {
                        FoundMethods.Add(assignToMethod);
                        goto NextMethod;
                    }
                }
                return false;
NextMethod:;
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
