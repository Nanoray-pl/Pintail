using System;
using System.Collections.Concurrent;
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

    /// <summary>
    /// Defines a configuration for <see cref="ProxyManager{Context}"/>.
    /// </summary>
    /// <typeparam name="Context">The context type used to describe the current proxy process. Use <see cref="Nothing"/> if not needed.</typeparam>
    public class ProxyManagerConfiguration<Context>
    {
        private static readonly MD5 MD5 = MD5.Create();

        private static string GetMd5String(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = MD5.ComputeHash(inputBytes);

            StringBuilder sb = new();
            for (int i = 0; i < hashBytes.Length; i++)
                sb.Append(hashBytes[i].ToString("X2"));
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
            IDictionary<string, string> qualifiedToShortName = new Dictionary<string, string>();
            IDictionary<string, int> shortNameCounts = new Dictionary<string, int>();

            return (moduleBuilder, proxyInfo) =>
            {
                lock (qualifiedToShortName)
                {
                    string qualifiedName = proxyInfo.GetNameSuitableForProxyTypeName(type => type.GetQualifiedName());
                    if (qualifiedToShortName.TryGetValue(qualifiedName, out string? shortName))
                        return shortName;

                    shortName = proxyInfo.GetNameSuitableForProxyTypeName(type => type.GetShortName());
                    if (!shortNameCounts.TryGetValue(shortName, out int shortNameCount))
                        shortNameCount = 0;

                    shortNameCount++;
                    shortNameCounts[shortName] = shortNameCount;
                    shortName = $"{shortName}_{shortNameCount}";
                    qualifiedToShortName[qualifiedName] = shortName;
                    return shortName;
                }
            };
        });

        /// <summary>
        /// The default <see cref="ProxyManagerNoMatchingMethodHandler{Context}"/> implementation.<br/>
        /// If a method cannot be implemented, <see cref="ArgumentException"/> will be thrown right away.
        /// </summary>
        public static readonly ProxyManagerNoMatchingMethodHandler<Context> ThrowExceptionNoMatchingMethodHandler = (proxyBuilder, proxyInfo, _, _, _, proxyMethod)
            => throw new ArgumentException($"The {proxyInfo.Proxy.Type.GetShortName()} interface defines method {proxyMethod.Name} which doesn't exist in the API or depends on an interface that cannot be mapped!");

        /// <summary>
        /// If a method cannot be implemented, a blank implementation will be created instead, which will throw <see cref="NotImplementedException"/> when called.
        /// </summary>
        public static readonly ProxyManagerNoMatchingMethodHandler<Context> ThrowingImplementationNoMatchingMethodHandler = (proxyBuilder, proxyInfo, _, _, _, proxyMethod) =>
        {
            MethodBuilder methodBuilder = proxyBuilder.DefineMethod(proxyMethod.Name, MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual);

            Type[] proxyGenericArguments = proxyMethod.GetGenericArguments();
            string[] genericArgNames = proxyGenericArguments.Select(a => a.Name).ToArray();
            GenericTypeParameterBuilder[] genericTypeParameterBuilders = proxyGenericArguments.Length == 0 ? Array.Empty<GenericTypeParameterBuilder>() : methodBuilder.DefineGenericParameters(genericArgNames);
            for (int i = 0; i < proxyGenericArguments.Length; i++)
                genericTypeParameterBuilders[i].SetGenericParameterAttributes(proxyGenericArguments[i].GenericParameterAttributes);

            Type returnType = proxyMethod.ReturnType.IsGenericMethodParameter ? genericTypeParameterBuilders[proxyMethod.ReturnType.GenericParameterPosition] : proxyMethod.ReturnType;
            methodBuilder.SetReturnType(returnType);

            Type[] argTypes = proxyMethod.GetParameters()
                .Select(a => a.ParameterType)
                .Select(t => t.IsGenericMethodParameter ? genericTypeParameterBuilders[t.GenericParameterPosition] : t)
                .ToArray();
            methodBuilder.SetParameters(argTypes);

            ILGenerator il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldstr, $"The {proxyInfo.Proxy.Type.GetShortName()} interface defines method {proxyMethod.Name} which doesn't exist in the API. (It may depend on an interface that was not mappable).");
            il.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor(new Type[] { typeof(string) })!);
            il.Emit(OpCodes.Throw);
        };

        /// <summary>
        /// The type name provider to use.
        /// </summary>
        public readonly ProxyManagerTypeNameProvider<Context> TypeNameProvider;

        /// <summary>
        /// The behavior to use if no matching method to proxy is found.
        /// </summary>
        public readonly ProxyManagerNoMatchingMethodHandler<Context> NoMatchingMethodHandler;

        /// <summary>
        /// When exactly proxy factories for interfaces and delegates should be created and prepared.
        /// </summary>
        public readonly ProxyManagerProxyPrepareBehavior ProxyPrepareBehavior;

        /// <summary>
        /// The behavior to use when mapping <see cref="Enum"/> arguments while matching methods to proxy.
        /// </summary>
        public readonly ProxyManagerEnumMappingBehavior EnumMappingBehavior;

        /// <summary>
        /// The behavior to use when mapping mismatched <see cref="Array"/> elements back and forth.
        /// </summary>
        public readonly ProxyManagerMismatchedArrayMappingBehavior MismatchedArrayMappingBehavior;

        /// <summary>
        /// Whether proxy types should implement any marker interfaces.
        /// </summary>
        public readonly ProxyObjectInterfaceMarking ProxyObjectInterfaceMarking;

        /// <summary>
        /// Defines whether access level checks should be enabled for generated proxy types.
        /// </summary>
        public readonly AccessLevelChecking AccessLevelChecking;

        /// <summary>
        /// Creates a new configuration for <see cref="ProxyManager{Context}"/>.
        /// </summary>
        /// <param name="typeNameProvider">The type name provider to use.<br/>Defaults to <see cref="ShortNameIDGeneratingTypeNameProvider"/>.</param>
        /// <param name="noMatchingMethodHandler">The behavior to use if no matching method to proxy is found.<br/>Defaults to <see cref="ThrowExceptionNoMatchingMethodHandler"/>.</param>
        /// <param name="proxyPrepareBehavior">When exactly proxy factories for interfaces and delegates should be created and prepared.<br/>Defaults to <see cref="ProxyManagerProxyPrepareBehavior.Lazy"/>.</param>
        /// <param name="enumMappingBehavior">The behavior to use when mapping <see cref="Enum"/> arguments while matching methods to proxy.<br/>Defaults to <see cref="ProxyManagerEnumMappingBehavior.ThrowAtRuntime"/>.</param>
        /// <param name="mismatchedArrayMappingBehavior">The behavior to use when mapping mismatched <see cref="Array"/> elements back and forth.<br/>Defaults to <see cref="ProxyManagerMismatchedArrayMappingBehavior.Throw"/>.</param>
        /// <param name="proxyObjectInterfaceMarking">Whether proxy types should implement any marker interfaces.<br/>Defaults to <see cref="ProxyObjectInterfaceMarking.Marker"/>.</param>
        /// <param name="accessLevelChecking">Defines whether access level checks should be enabled for generated proxy types.<br/>Defaults to <see cref="AccessLevelChecking.Enabled"/>.</param>
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
            this.NoMatchingMethodHandler = noMatchingMethodHandler ?? ThrowExceptionNoMatchingMethodHandler;
            this.ProxyPrepareBehavior = proxyPrepareBehavior;
            this.EnumMappingBehavior = enumMappingBehavior;
            this.MismatchedArrayMappingBehavior = mismatchedArrayMappingBehavior;
            this.ProxyObjectInterfaceMarking = proxyObjectInterfaceMarking;
            this.AccessLevelChecking = accessLevelChecking;
        }
    }

    /// <summary>
    /// The default <see cref="IProxyManager{Context}"/> implementation.
    /// </summary>
    /// <typeparam name="Context">The context type used to describe the current proxy process. Use <see cref="Nothing"/> if not needed.</typeparam>
    public sealed class ProxyManager<Context>: IProxyManager<Context>
    {
        internal readonly ModuleBuilder ModuleBuilder;
        internal readonly ProxyManagerConfiguration<Context> Configuration;
        private readonly Dictionary<ProxyInfo<Context>, IProxyFactory<Context>> Factories = new();
        private readonly ConcurrentDictionary<string, List<Type>> InterfaceMappabilityCache = new();
        private readonly Dictionary<ProxyInfo<Context>, Exception> FailedProxyTypeExceptions = new();

        /// <summary>
        /// Constructs a <see cref="ProxyManager{Context}"/>.
        /// </summary>
        /// <param name="moduleBuilder">The <see cref="System.Reflection.Emit.ModuleBuilder"/> to use for creating the proxy types in.</param>
        /// <param name="configuration">Configuration to use for this <see cref="ProxyManager{Context}"/>. Defaults to `null`, which means that the default configuration will be used.</param>
        public ProxyManager(ModuleBuilder moduleBuilder, ProxyManagerConfiguration<Context>? configuration = null)
        {
            this.ModuleBuilder = moduleBuilder;
            this.Configuration = configuration ?? new();
        }

        /// <inheritdoc/>
        public IProxyFactory<Context>? GetProxyFactory(ProxyInfo<Context> proxyInfo)
        {
            lock (this.Factories)
            {
                if (this.Factories.TryGetValue(proxyInfo, out IProxyFactory<Context>? factory))
                    return factory;
            }
            return null;
        }

        /// <inheritdoc/>
        public IProxyFactory<Context> ObtainProxyFactory(ProxyInfo<Context> proxyInfo)
        {
            lock (this.Factories)
            {
                if (this.FailedProxyTypeExceptions.TryGetValue(proxyInfo, out var priorException))
                    throw new ArgumentException($"Unhandled proxy/conversion method for info: {proxyInfo}", priorException);

                if (!this.Factories.TryGetValue(proxyInfo, out IProxyFactory<Context>? factory))
                {
                    if (proxyInfo.Target.Type == proxyInfo.Proxy.Type)
                    {
                        factory = new NoOpProxyFactory<Context>(proxyInfo);
                        this.Factories[proxyInfo] = factory;
                    }
                    else if (proxyInfo.Target.Type.IsEnum && proxyInfo.Proxy.Type.IsEnum)
                    {
                        factory = new EnumProxyFactory<Context>(proxyInfo);
                        this.Factories[proxyInfo] = factory;
                    }
                    else if (proxyInfo.Target.Type.IsArray && proxyInfo.Proxy.Type.IsArray)
                    {
                        factory = new ArrayProxyFactory<Context>(proxyInfo, this.Configuration.MismatchedArrayMappingBehavior);
                        this.Factories[proxyInfo] = factory;
                    }
                    else if (proxyInfo.Target.Type.IsConstructedGenericType && proxyInfo.Proxy.Type.IsConstructedGenericType && proxyInfo.Target.Type.GetGenericTypeDefinition() == typeof(Nullable<>) && proxyInfo.Proxy.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        factory = new NullableProxyFactory<Context>(proxyInfo);
                        this.Factories[proxyInfo] = factory;
                    }
                    else if (proxyInfo.Proxy.Type.IsInterface || (proxyInfo.Proxy.Type.IsAssignableTo(typeof(Delegate)) && proxyInfo.Target.Type.IsAssignableTo(typeof(Delegate))))
                    {
                        var newFactory = new InterfaceOrDelegateProxyFactory<Context>(
                            proxyInfo,
                            this.Configuration.NoMatchingMethodHandler,
                            this.Configuration.ProxyPrepareBehavior,
                            this.Configuration.EnumMappingBehavior,
                            this.Configuration.ProxyObjectInterfaceMarking,
                            this.Configuration.AccessLevelChecking,
                            this.InterfaceMappabilityCache
                        );
                        factory = newFactory;
                        this.Factories[proxyInfo] = factory;
                        try
                        {
                            newFactory.Prepare(this, this.Configuration.TypeNameProvider(this.ModuleBuilder, proxyInfo));
                        }
                        catch (Exception e)
                        {
                            this.Factories.Remove(proxyInfo);
                            this.FailedProxyTypeExceptions[proxyInfo] = e;
                            throw new ArgumentException($"Unhandled proxy/conversion method for info: {proxyInfo}", e);
                        }
                    }
                    else
                    {
                        var newFactory = new ReconstructingProxyFactory<Context>(proxyInfo, this.Configuration.EnumMappingBehavior, this.Configuration.AccessLevelChecking, this.InterfaceMappabilityCache);
                        factory = newFactory;
                        this.Factories[proxyInfo] = factory;
                        try
                        {
                            newFactory.Prepare();
                        }
                        catch (Exception e)
                        {
                            this.Factories.Remove(proxyInfo);
                            this.FailedProxyTypeExceptions[proxyInfo] = e;
                            throw new ArgumentException($"Unhandled proxy/conversion method for info: {proxyInfo}", e);
                        }
                    }
                }
                return factory;
            }
        }

        /// <inheritdoc/>
        public IProxyFactory<Context>? TryObtainProxyFactory(ProxyInfo<Context> proxyInfo)
        {
            lock (this.Factories)
            {
                if (this.FailedProxyTypeExceptions.ContainsKey(proxyInfo))
                    return null;
                return this.ObtainProxyFactory(proxyInfo);
            }
        }
    }
}
