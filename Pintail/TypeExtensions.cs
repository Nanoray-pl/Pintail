using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nanoray.Pintail
{
    internal static class TypeExtensions
    {
        [ThreadStatic]
        public static Type?[]?[]? TypeArrays;

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
            return BuildTypeName(type, type => type.AssemblyQualifiedName ?? $"{type.Assembly.GetName().FullName}@@{type.FullName ?? type.Name}");
        }

        internal static string GetShortName(this Type type)
        {
            return BuildTypeName(type, type => type.Name);
        }

        private static string BuildTypeName(Type type, Func<Type, string> nameProvider)
        {
            StringBuilder sb = new(nameProvider(type));
            var genericArguments = type.GetGenericArguments();
            if (genericArguments.Length != 0)
            {
                sb.Append('[');
                sb.AppendJoin(",", genericArguments.Select(nameProvider));
                sb.Append(']');
            }
            return sb.ToString();
        }

        private static IEnumerable<Type> GetInterfacesRecursivelyAsEnumerable(this Type type, bool includingSelf)
        {
            if (includingSelf && type.IsInterface)
                yield return type;

            foreach (var interfaceType in type.GetInterfaces())
            {
                yield return interfaceType;
                foreach (var recursiveInterfaceType in interfaceType.GetInterfacesRecursivelyAsEnumerable(false))
                    yield return recursiveInterfaceType;
            }
        }

        internal static IEnumerable<Enum> GetEnumerableEnumValues(this Type type)
        {
            if (!type.IsEnum)
                throw new ArgumentException($"{type.GetShortName()} is not an enum.");
            foreach (object value in Enum.GetValues(type))
                yield return (Enum)value;
        }

        internal static IEnumerable<EnumType> GetEnumerableEnumValues<EnumType>() where EnumType : Enum
        {
            return typeof(EnumType).GetEnumerableEnumValues().Select(e => (EnumType)e);
        }

        internal static Type ReplacingGenericArguments(this Type self, Dictionary<string, Type>? realGenericArguments)
        {
            if (realGenericArguments is null)
                return self;
            if (!self.ContainsGenericParameters)
                return self;
            if (self is { IsGenericParameter: true, FullName: null } && realGenericArguments.TryGetValue(self.Name, out var replacementType))
                return replacementType;

            var genericTypeArguments = self.GenericTypeArguments;
            var typeArray = GetTypeArray(genericTypeArguments.Length);

            for (int i = 0; i < genericTypeArguments.Length; i++)
                typeArray[i] = genericTypeArguments[i].ReplacingGenericArguments(realGenericArguments);
            return self.GetGenericTypeDefinition().MakeGenericType(typeArray);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Type[] GetTypeArray(int length)
        {
            TypeArrays ??= new Type[16][];
            if (length > TypeArrays.Length)
                Array.Resize(ref TypeArrays, length);
            TypeArrays[length] ??= new Type[length];
            return TypeArrays[length]!;
        }
    }
}
