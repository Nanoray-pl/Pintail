using System;
using System.Collections.Generic;
using System.Linq;

namespace Nanoray.Pintail
{
    internal static class TypeExtensions
    {
        internal static Type GetNonRefType(this Type type)
        {
            return type.IsByRef ? type.GetElementOrSelfType() : type;
        }

        internal static Type GetElementOrSelfType(this Type type)
        {
            return type.GetElementType() ?? type;
        }

        // TODO: remove when arrays are implemented properly
        internal static Type GetRecursiveElementOrSelfType(this Type type)
        {
            Type result = type;
            while (true)
            {
                Type? elementType = result.GetElementType();
                if (elementType == null)
                    break;
                else
                    result = elementType;
            }
            return result;
        }

        internal static ISet<Type> GetInterfacesRecursively(this Type type, bool includingSelf)
        {
            return type.GetInterfacesRecursivelyAsEnumerable(includingSelf).ToHashSet();
        }

        internal static string GetBestName(this Type type)
        {
            return type.FullName ?? type.Name;
        }

        private static IEnumerable<Type> GetInterfacesRecursivelyAsEnumerable(this Type type, bool includingSelf)
        {
            if (includingSelf && type.IsInterface)
                yield return type;
            foreach (Type interfaceType in type.GetInterfaces())
            {
                yield return interfaceType;
                foreach (Type recursiveInterfaceType in interfaceType.GetInterfacesRecursivelyAsEnumerable(false))
                {
                    yield return recursiveInterfaceType;
                }
            }
        }

        internal static IEnumerable<Enum> GetEnumerableEnumValues(this Type type)
        {
            if (!type.IsEnum)
                throw new ArgumentException($"{type.GetBestName()} is not an enum.");
            foreach (object value in Enum.GetValues(type))
                yield return (Enum)value;
        }

        internal static IEnumerable<EnumType> GetEnumerableEnumValues<EnumType>() where EnumType: Enum
        {
            return typeof(EnumType).GetEnumerableEnumValues().Select(e => (EnumType)e);
        }
    }
}
