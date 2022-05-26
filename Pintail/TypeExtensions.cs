using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        internal static ISet<Type> GetInterfacesRecursively(this Type type, bool includingSelf)
        {
            return type.GetInterfacesRecursivelyAsEnumerable(includingSelf).ToHashSet();
        }

        internal static string GetQualifiedName(this Type type)
        {
            string? fullName = type.AssemblyQualifiedName;
            if (fullName is not null)
                return fullName;
            return BuildQualifiedTypeName(type, type => $"[{type.GetQualifiedName()}]");
        }

        internal static string GetShortName(this Type type)
        {
            return BuildTypeName(type, GetShortName);
        }

        private static string BuildQualifiedTypeName(Type type, Func<Type, string> nameProvider)
        {
            StringBuilder sb = new(type.Assembly.GetName().FullName);
            sb.Append("@@").Append(type.FullName);
            Type[] genericArguments = type.GetGenericArguments();
            if (genericArguments.Length != 0)
            {
                sb.Append('[');
                sb.AppendJoin(",", genericArguments.Select(generic => nameProvider(generic)));
                sb.Append(']');
            }
            return sb.ToString();
        }

        private static string BuildTypeName(Type type, Func<Type, string> nameProvider)
        {
            StringBuilder sb = new(type.Name);
            Type[] genericArguments = type.GetGenericArguments();
            if (genericArguments.Length != 0)
            {
                sb.Append('[');
                sb.AppendJoin(",", genericArguments.Select(generic => nameProvider(generic)));
                sb.Append(']');
            }
            return sb.ToString();
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
                throw new ArgumentException($"{type.GetShortName()} is not an enum.");
            foreach (object value in Enum.GetValues(type))
                yield return (Enum)value;
        }

        internal static IEnumerable<EnumType> GetEnumerableEnumValues<EnumType>() where EnumType: Enum
        {
            return typeof(EnumType).GetEnumerableEnumValues().Select(e => (EnumType)e);
        }

        internal static Type ReplacingGenericArguments(this Type self, IDictionary<string, Type> realGenericArguments)
        {
            if (!self.ContainsGenericParameters)
                return self;
            if (self.IsGenericParameter && self.FullName is null && realGenericArguments.TryGetValue(self.Name, out Type? replacementType) && replacementType is not null)
                return replacementType;

            Type[] genericArguments = self.GenericTypeArguments.Select(t => t.ReplacingGenericArguments(realGenericArguments)).ToArray();
            return genericArguments.Length == 0 ? self : self.GetGenericTypeDefinition().MakeGenericType(genericArguments);
        }
    }
}
