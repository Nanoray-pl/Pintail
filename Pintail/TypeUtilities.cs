using System;
using System.Linq;

namespace Nanoray.Pintail
{
    internal static class TypeUtilities
    {
        internal enum MethodTypeMatchingPart { ReturnType, Parameter }
        internal enum MatchingTypesResult { False, IfProxied, True }

        internal static MatchingTypesResult AreTypesMatching(Type targetType, Type proxyType, MethodTypeMatchingPart part, DefaultProxyManagerEnumMappingBehavior enumMappingBehavior)
        {
            var typeA = part == MethodTypeMatchingPart.Parameter ? targetType : proxyType;
            var typeB = part == MethodTypeMatchingPart.Parameter ? proxyType : targetType;

            if (typeA.IsGenericMethodParameter != typeB.IsGenericMethodParameter)
                return MatchingTypesResult.False;
            if (proxyType.IsEnum && targetType.IsEnum)
            {
                if (proxyType == targetType)
                    return MatchingTypesResult.True;
                var proxyEnumRawValues = proxyType.GetEnumerableEnumValues().Select(e => (int)(object)e).ToList();
                var targetEnumRawValues = targetType.GetEnumerableEnumValues().Select(e => (int)(object)e).ToList();
                switch (enumMappingBehavior)
                {
                    case DefaultProxyManagerEnumMappingBehavior.Strict:
                        return proxyEnumRawValues.OrderBy(e => e).SequenceEqual(targetEnumRawValues.OrderBy(e => e)) ? MatchingTypesResult.IfProxied : MatchingTypesResult.False;
                    case DefaultProxyManagerEnumMappingBehavior.AllowAdditive:
                        return targetEnumRawValues.ToHashSet().Except(proxyEnumRawValues).Any() ? MatchingTypesResult.False : MatchingTypesResult.IfProxied;
                    case DefaultProxyManagerEnumMappingBehavior.ThrowAtRuntime:
                        return MatchingTypesResult.IfProxied;
                }
            }
            if (proxyType.IsArray && targetType.IsArray)
                return proxyType == targetType ? MatchingTypesResult.True : MatchingTypesResult.IfProxied;
            if (typeA.IsGenericMethodParameter)
                return typeA.GenericParameterPosition == typeB.GenericParameterPosition ? MatchingTypesResult.True : MatchingTypesResult.False;

            if (typeA.IsAssignableFrom(typeB))
                return MatchingTypesResult.True;

            if (proxyType.GetNonRefType().IsInterface)
                return MatchingTypesResult.IfProxied;
            if (targetType.GetNonRefType().IsInterface)
                return MatchingTypesResult.IfProxied;

            var targetTypeGenericArguments = targetType.GetGenericArguments();
            var proxyTypeGenericArguments = proxyType.GetGenericArguments();
            if (targetTypeGenericArguments.Length != proxyTypeGenericArguments.Length || targetTypeGenericArguments.Length == 0)
                return MatchingTypesResult.False;

            var genericTargetType = targetType.GetGenericTypeDefinition();
            var genericProxyType = proxyType.GetGenericTypeDefinition();

            var matchingTypesResult = MatchingTypesResult.True;
            switch (AreTypesMatching(genericTargetType, genericProxyType, part, enumMappingBehavior))
            {
                case MatchingTypesResult.True:
                    break;
                case MatchingTypesResult.IfProxied:
                    matchingTypesResult = MatchingTypesResult.IfProxied;
                    break;
                case MatchingTypesResult.False:
                    return MatchingTypesResult.False;
            }
            for (int i = 0; i < targetTypeGenericArguments.Length; i++)
            {
                switch (AreTypesMatching(targetTypeGenericArguments[i], proxyTypeGenericArguments[i], part, enumMappingBehavior))
                {
                    case MatchingTypesResult.True:
                        break;
                    case MatchingTypesResult.IfProxied:
                        matchingTypesResult = MatchingTypesResult.IfProxied;
                        break;
                    case MatchingTypesResult.False:
                        return MatchingTypesResult.False;
                }
            }
            return matchingTypesResult;
        }
    }
}
