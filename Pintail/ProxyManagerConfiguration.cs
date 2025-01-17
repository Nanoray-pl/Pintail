﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;

namespace Nanoray.Pintail
{
    /// <summary>
    /// A type which provides type names for proxies created by the <see cref="ProxyManager{Context}"/>.
    /// </summary>
    /// <typeparam name="Context">The context type used to describe the current proxy process. Use <see cref="Nothing"/> if not needed.</typeparam>
    public delegate string ProxyManagerTypeNameProvider<Context>(ModuleBuilder moduleBuilder, ProxyInfo<Context> proxyInfo);

    /// <summary>
    /// A type which defines the behavior to use if a given proxy method could not be implemented when using <see cref="ProxyManager{Context}"/>.
    /// The delegate will be called early, before a proxy type is defined.
    /// </summary>
    /// <typeparam name="Context">The context type used to describe the current proxy process. Use <see cref="Nothing"/> if not needed.</typeparam>
    public delegate void EarlyProxyManagerNoMatchingMethodHandler<Context>(ProxyInfo<Context> proxyInfo, MethodInfo proxyMethod);

    /// <summary>
    /// A type which defines the behavior to use if a given proxy method could not be implemented when using <see cref="ProxyManager{Context}"/>.
    /// The delegate will be called late, after a proxy type is defined.
    /// </summary>
    /// <typeparam name="Context">The context type used to describe the current proxy process. Use <see cref="Nothing"/> if not needed.</typeparam>
    public delegate void ProxyManagerNoMatchingMethodHandler<Context>(TypeBuilder proxyBuilder, ProxyInfo<Context> proxyInfo, FieldBuilder targetField, FieldBuilder glueField, FieldBuilder proxyInfosField, MethodInfo proxyMethod);

    /// <summary>
    /// Defines when proxy factories for interfaces and delegates should be created and prepared.
    /// </summary>
    public enum ProxyManagerProxyPrepareBehavior
    {
        /// <summary>
        /// Create and prepare proxy factories the first time they are seen.
        /// </summary>
        /// <remarks>Generic types using generic method arguments are unknown at that time, so they will still be created lazily.</remarks>
        Eager,

        /// <summary>
        /// Create and prepare proxy factories when they are actually needed (when a method using one is first called).
        /// </summary>
        Lazy
    }

    /// <summary>
    /// Defines the behavior to use when mapping <see cref="Enum"/> arguments while matching methods to proxy.
    /// </summary>
    public enum ProxyManagerEnumMappingBehavior
    {
        /// <summary>
        /// Only allow 1:1 mappings; don't match otherwise.
        /// </summary>
        Strict,

        /// <summary>
        /// Allow mappings where the proxy <see cref="Enum"/> has extra values not found in the target <see cref="Enum"/>.
        /// </summary>
        AllowAdditive,

        /// <summary>
        /// Allow all mappings; throw <see cref="ArgumentException"/> if it couldn't be mapped at runtime.
        /// </summary>
        ThrowAtRuntime
    }

    /// <summary>
    /// Defines the behavior to use when mapping mismatched <see cref="Array"/> elements back and forth.
    /// </summary>
    public enum ProxyManagerMismatchedArrayMappingBehavior
    {
        /// <summary>
        /// Throw <see cref="ArgumentException"/> when passing in an array that cannot exactly be proxied back.
        /// </summary>
        Throw,

        /// <summary>
        /// Allow mismatched array types; do not map array elements back.
        /// </summary>
        AllowAndDontMapBack
    }

    file static class ProxyManagerConfiguration
    {
        public static readonly MD5 MD5 = MD5.Create();
    }

    /// <summary>
    /// Defines a configuration for <see cref="ProxyManager{Context}"/>.
    /// </summary>
    /// <typeparam name="Context">The context type used to describe the current proxy process. Use <see cref="Nothing"/> if not needed.</typeparam>
    public sealed class ProxyManagerConfiguration<Context>
    {
        private static string GetMd5String(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = ProxyManagerConfiguration.MD5.ComputeHash(inputBytes);

            StringBuilder sb = new();
            foreach (byte b in hashBytes)
                sb.Append(b.ToString("X2"));
            return sb.ToString();
        }

        /// <summary>
        /// A <see cref="ProxyManagerTypeNameProvider{Context}"/> implementation using full (qualified) type names.
        /// </summary>
        public static readonly ProxyManagerTypeNameProvider<Context> FullNameTypeNameProvider = (moduleBuilder, proxyInfo)
            => $"{moduleBuilder.Assembly.GetName().Name}.{proxyInfo.GetNameSuitableForProxyTypeName(type => type.GetQualifiedName())}";

        /// <summary>
        /// A <see cref="ProxyManagerTypeNameProvider{Context}"/> implementation using short type names.
        /// </summary>
        [Obsolete("This type name provider is not recommended, due to potential proxy type name conflicts.")]
        public static readonly ProxyManagerTypeNameProvider<Context> ShortNameTypeNameProvider = (moduleBuilder, proxyInfo)
            => $"{moduleBuilder.Assembly.GetName().Name}.{proxyInfo.GetNameSuitableForProxyTypeName(type => type.Name)}";

        /// <summary>
        /// A <see cref="ProxyManagerTypeNameProvider{Context}"/> implementation using MD5 hashes.
        /// </summary>
        public static readonly ProxyManagerTypeNameProvider<Context> Md5TypeNameProvider = (moduleBuilder, proxyInfo)
            => $"{moduleBuilder.Assembly.GetName().Name}.{proxyInfo.GetNameSuitableForProxyTypeName(type => GetMd5String(type.GetQualifiedName()))}";

        /// <summary>
        /// A <see cref="ProxyManagerTypeNameProvider{Context}"/> implementation using short type names with appended generated IDs to avoid conflicts.
        /// </summary>
        public static readonly Lazy<ProxyManagerTypeNameProvider<Context>> ShortNameIDGeneratingTypeNameProvider = new(() =>
        {
            var qualifiedToShortName = new Dictionary<string, string>();
            var shortNameCounts = new Dictionary<string, int>();

            return (_, proxyInfo) =>
            {
                lock (qualifiedToShortName)
                {
                    string qualifiedName = proxyInfo.GetNameSuitableForProxyTypeName(type => type.GetQualifiedName());
                    if (qualifiedToShortName.TryGetValue(qualifiedName, out string? shortName))
                        return shortName;

                    shortName = proxyInfo.GetNameSuitableForProxyTypeName(type => type.GetShortName());
                    int shortNameCount = shortNameCounts.GetValueOrDefault(shortName);

                    shortNameCount++;
                    shortNameCounts[shortName] = shortNameCount;
                    shortName = $"{shortName}_{shortNameCount}";
                    qualifiedToShortName[qualifiedName] = shortName;
                    return shortName;
                }
            };
        });

        /// <summary>
        /// The default <see cref="EarlyProxyManagerNoMatchingMethodHandler{Context}"/> implementation.<br/>
        /// If a method cannot be implemented, <see cref="ArgumentException"/> will be thrown right away.
        /// </summary>
        public static readonly EarlyProxyManagerNoMatchingMethodHandler<Context> ThrowExceptionNoMatchingMethodHandler = (proxyInfo, proxyMethod)
            => throw new ArgumentException($"The {proxyInfo.Proxy.Type.GetShortName()} interface defines method {proxyMethod.Name} which doesn't exist in the API or depends on an interface that cannot be mapped!");

        /// <summary>
        /// The default <see cref="ProxyManagerNoMatchingMethodHandler{Context}"/> implementation.<br/>
        /// If a method cannot be implemented, a blank implementation will be created instead, which will throw <see cref="NotImplementedException"/> when called.
        /// </summary>
        public static readonly ProxyManagerNoMatchingMethodHandler<Context> ThrowingImplementationNoMatchingMethodHandler = (proxyBuilder, proxyInfo, _, _, _, proxyMethod) =>
        {
            var methodBuilder = proxyBuilder.DefineMethod(proxyMethod.Name, MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual);

            var proxyGenericArguments = proxyMethod.GetGenericArguments();
            string[] genericArgNames = proxyGenericArguments.Select(a => a.Name).ToArray();
            var genericTypeParameterBuilders = proxyGenericArguments.Length == 0 ? [] : methodBuilder.DefineGenericParameters(genericArgNames);
            for (int i = 0; i < proxyGenericArguments.Length; i++)
                genericTypeParameterBuilders[i].SetGenericParameterAttributes(proxyGenericArguments[i].GenericParameterAttributes);

            var returnType = proxyMethod.ReturnType.IsGenericMethodParameter ? genericTypeParameterBuilders[proxyMethod.ReturnType.GenericParameterPosition] : proxyMethod.ReturnType;
            methodBuilder.SetReturnType(returnType);

            var argTypes = proxyMethod.GetParameters()
                .Select(a => a.ParameterType)
                .Select(t => t.IsGenericMethodParameter ? genericTypeParameterBuilders[t.GenericParameterPosition] : t)
                .ToArray();
            methodBuilder.SetParameters(argTypes);

            var il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldstr, $"The {proxyInfo.Proxy.Type.GetShortName()} interface defines method {proxyMethod.Name} which doesn't exist in the API. (It may depend on an interface that was not mappable).");
            il.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor([typeof(string)])!);
            il.Emit(OpCodes.Throw);
        };

        /// <summary>
        /// The type name provider to use.
        /// </summary>
        public ProxyManagerTypeNameProvider<Context> TypeNameProvider { get; init; } = ShortNameIDGeneratingTypeNameProvider.Value;

        /// <summary>
        /// The behavior to use if no matching method to proxy is found.
        /// The delegate will be called early, before a proxy type is defined.
        /// </summary>
        public EarlyProxyManagerNoMatchingMethodHandler<Context>? EarlyNoMatchingMethodHandler { get; init; } = ThrowExceptionNoMatchingMethodHandler;

        /// <summary>
        /// The behavior to use if no matching method to proxy is found.
        /// The delegate will be called late, after a proxy type is defined.
        /// </summary>
        public ProxyManagerNoMatchingMethodHandler<Context> NoMatchingMethodHandler { get; init; } = ThrowingImplementationNoMatchingMethodHandler;

        /// <summary>
        /// When exactly proxy factories for interfaces and delegates should be created and prepared.
        /// </summary>
        public ProxyManagerProxyPrepareBehavior ProxyPrepareBehavior { get; init; } = ProxyManagerProxyPrepareBehavior.Lazy;

        /// <summary>
        /// The behavior to use when mapping <see cref="Enum"/> arguments while matching methods to proxy.
        /// </summary>
        public ProxyManagerEnumMappingBehavior EnumMappingBehavior { get; init; } = ProxyManagerEnumMappingBehavior.ThrowAtRuntime;

        /// <summary>
        /// The behavior to use when mapping mismatched <see cref="Array"/> elements back and forth.
        /// </summary>
        public ProxyManagerMismatchedArrayMappingBehavior MismatchedArrayMappingBehavior { get; init; } = ProxyManagerMismatchedArrayMappingBehavior.Throw;

        /// <summary>
        /// Whether proxy types should implement any marker interfaces.
        /// </summary>
        public ProxyObjectInterfaceMarking ProxyObjectInterfaceMarking { get; init; } = ProxyObjectInterfaceMarking.Marker;

        /// <summary>
        /// Defines whether access level checks should be enabled for generated proxy types.
        /// </summary>
        public AccessLevelChecking AccessLevelChecking { get; init; } = AccessLevelChecking.Enabled;

        /// <summary>
        /// Creates a new configuration for <see cref="ProxyManager{Context}"/>.
        /// All configuration is meant to be declared in an object initializer.
        /// </summary>
        public ProxyManagerConfiguration()
        {
        }

        /// <summary>
        /// Creates a new configuration for <see cref="ProxyManager{Context}"/>.
        /// </summary>
        /// <param name="typeNameProvider">The type name provider to use.<br/>Defaults to <see cref="ShortNameIDGeneratingTypeNameProvider"/>.</param>
        /// <param name="noMatchingMethodHandler">The behavior to use if no matching method to proxy is found.<br/>Defaults to <see cref="ThrowingImplementationNoMatchingMethodHandler"/>.</param>
        /// <param name="proxyPrepareBehavior">When exactly proxy factories for interfaces and delegates should be created and prepared.<br/>Defaults to <see cref="ProxyManagerProxyPrepareBehavior.Lazy"/>.</param>
        /// <param name="enumMappingBehavior">The behavior to use when mapping <see cref="Enum"/> arguments while matching methods to proxy.<br/>Defaults to <see cref="ProxyManagerEnumMappingBehavior.ThrowAtRuntime"/>.</param>
        /// <param name="mismatchedArrayMappingBehavior">The behavior to use when mapping mismatched <see cref="Array"/> elements back and forth.<br/>Defaults to <see cref="ProxyManagerMismatchedArrayMappingBehavior.Throw"/>.</param>
        /// <param name="proxyObjectInterfaceMarking">Whether proxy types should implement any marker interfaces.<br/>Defaults to <see cref="ProxyObjectInterfaceMarking.Marker"/>.</param>
        /// <param name="accessLevelChecking">Defines whether access level checks should be enabled for generated proxy types.<br/>Defaults to <see cref="AccessLevelChecking.Enabled"/>.</param>
        [Obsolete("Use the no parameter constructor instead and declare the configuration via an object initializer.")]
        public ProxyManagerConfiguration(
            ProxyManagerTypeNameProvider<Context>? typeNameProvider = null,
            ProxyManagerNoMatchingMethodHandler<Context>? noMatchingMethodHandler = null,
            ProxyManagerProxyPrepareBehavior proxyPrepareBehavior = ProxyManagerProxyPrepareBehavior.Lazy,
            ProxyManagerEnumMappingBehavior enumMappingBehavior = ProxyManagerEnumMappingBehavior.ThrowAtRuntime,
            ProxyManagerMismatchedArrayMappingBehavior mismatchedArrayMappingBehavior = ProxyManagerMismatchedArrayMappingBehavior.Throw,
            ProxyObjectInterfaceMarking proxyObjectInterfaceMarking = ProxyObjectInterfaceMarking.Marker,
            AccessLevelChecking accessLevelChecking = AccessLevelChecking.Enabled
        )
        {
            this.TypeNameProvider = typeNameProvider ?? ShortNameIDGeneratingTypeNameProvider.Value;
            this.EarlyNoMatchingMethodHandler = ThrowExceptionNoMatchingMethodHandler;
            this.NoMatchingMethodHandler = noMatchingMethodHandler ?? ThrowingImplementationNoMatchingMethodHandler;
            this.ProxyPrepareBehavior = proxyPrepareBehavior;
            this.EnumMappingBehavior = enumMappingBehavior;
            this.MismatchedArrayMappingBehavior = mismatchedArrayMappingBehavior;
            this.ProxyObjectInterfaceMarking = proxyObjectInterfaceMarking;
            this.AccessLevelChecking = accessLevelChecking;
        }
    }
}
