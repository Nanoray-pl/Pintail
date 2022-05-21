using System;
using System.Linq;
using System.Reflection;

namespace Nanoray.Pintail
{
    internal static class TypeUtilities
    {
        internal enum MethodTypeMatchingPart{ ReturnType, Parameter }
        internal enum MatchingTypesResult { False, IfProxied, Assignable, Exact }
        // Assignable is not currently supported.
        internal enum PositionConversion { Proxy }

        internal static MatchingTypesResult AreTypesMatching(Type targetType, Type proxyType, MethodTypeMatchingPart part, ProxyManagerEnumMappingBehavior enumMappingBehavior)
        {
            if (targetType.IsGenericMethodParameter != proxyType.IsGenericMethodParameter)
                return MatchingTypesResult.False;

            if (targetType.IsByRef != proxyType.IsByRef)
                return MatchingTypesResult.False;

            // toss the by ref ness? Not sure here.
            if (targetType.IsByRef && proxyType.IsByRef)
            {
                targetType = targetType.GetNonRefType();
                proxyType = proxyType.GetNonRefType();
            }

            var typeA = part == MethodTypeMatchingPart.Parameter ? targetType : proxyType;
            var typeB = part == MethodTypeMatchingPart.Parameter ? proxyType : targetType;

            if (proxyType.IsEnum && targetType.IsEnum)
            {
                if (proxyType == targetType)
                    return MatchingTypesResult.Exact;
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
            {
                if (proxyType == targetType)
                    return MatchingTypesResult.Exact;
                if (proxyType.GetElementType()!.IsInterface || proxyType.GetElementType()!.IsInterface)
                    return MatchingTypesResult.IfProxied;
                return AreTypesMatching(targetType.GetElementType()!, proxyType.GetElementType()!, part, enumMappingBehavior);
            }

            if (typeA.IsGenericMethodParameter)
                return typeA.GenericParameterPosition == typeB.GenericParameterPosition ? MatchingTypesResult.Exact : MatchingTypesResult.False;

            if (proxyType == targetType)
                return MatchingTypesResult.Exact;

            // not convinced this works well for ref/out params????
            if (typeA.IsAssignableFrom(typeB))
                return MatchingTypesResult.Assignable;

            if (proxyType.IsInterface || targetType.IsInterface)
                return MatchingTypesResult.IfProxied;

            var targetTypeGenericArguments = targetType.GetGenericArguments();
            var proxyTypeGenericArguments = proxyType.GetGenericArguments();
            if (targetTypeGenericArguments.Length != proxyTypeGenericArguments.Length || targetTypeGenericArguments.Length == 0)
                return MatchingTypesResult.False;

            var matchingTypesResult = MatchingTypesResult.Exact;

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
                            matchingTypesResult = (MatchingTypesResult)Math.Min((int)matchingTypesResult, (int)MatchingTypesResult.False);
                            break;
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
                        matchingTypesResult = (MatchingTypesResult)Math.Min((int)matchingTypesResult, (int)MatchingTypesResult.False);
                        break;
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
    }
}
