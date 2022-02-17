using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;

namespace Nanoray.Pintail
{
    /// <summary>
    /// A type which provides type names for proxies created by the <see cref="DefaultProxyManager{}"/>.
    /// </summary>
    /// <typeparam name="Context">The context type used to describe the current proxy process. Use <see cref="Nothing"/> if not needed.</typeparam>
    public delegate string DefaultProxyManagerTypeNameProvider<Context>(ModuleBuilder moduleBuilder, ProxyInfo<Context> proxyInfo);

    /// <summary>
    /// A type which defines the behavior to use if a given proxy method could not be implemented when using <see cref="DefaultProxyManager{}"/>.
    /// </summary>
    /// <typeparam name="Context">The context type used to describe the current proxy process. Use <see cref="Nothing"/> if not needed.</typeparam>
    public delegate void DefaultProxyManagerNoMatchingMethodHandler<Context>(TypeBuilder proxyBuilder, ProxyInfo<Context> proxyInfo, FieldBuilder targetField, FieldBuilder glueField, FieldBuilder proxyInfosField, MethodInfo proxyMethod);

    /// <summary>
    /// Defines the behavior to use when mapping <see cref="Enum"/> arguments while matching methods to proxy.
    /// </summary>
    public enum DefaultProxyManagerEnumMappingBehavior
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
    /// Defines a configuration for <see cref="DefaultProxyManager{}"/>.
    /// </summary>
    /// <typeparam name="Context">The context type used to describe the current proxy process. Use <see cref="Nothing"/> if not needed.</typeparam>
    public class DefaultProxyManagerConfiguration<Context>
    {
        private static readonly MD5 MD5 = MD5.Create();

        private static string GetMd5String(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = MD5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
                sb.Append(hashBytes[i].ToString("X2"));
            return sb.ToString();
        }

        /// <summary>
        /// A <see cref="DefaultProxyManagerTypeNameProvider{}"/> implementation using full type names.
        /// </summary>
        public static readonly DefaultProxyManagerTypeNameProvider<Context> FullNameTypeNameProvider = (moduleBuilder, proxyInfo)
            => $"{moduleBuilder.FullyQualifiedName}.From<<{proxyInfo.Proxy.Context}>_<{proxyInfo.Proxy.Type.GetBestName()}>>_To<<{proxyInfo.Target.Context}>_<{proxyInfo.Target.Type.GetBestName()}>>";

        /// <summary>
        /// A <see cref="DefaultProxyManagerTypeNameProvider{}"/> implementation using short type names.
        /// </summary>
        public static readonly DefaultProxyManagerTypeNameProvider<Context> ShortNameTypeNameProvider = (moduleBuilder, proxyInfo)
            => $"{moduleBuilder.FullyQualifiedName}.From<<{proxyInfo.Proxy.Context}>_<{proxyInfo.Proxy.Type.Name}>>_To<<{proxyInfo.Target.Context}>_<{proxyInfo.Target.Type.Name}>>";

        /// <summary>
        /// A <see cref="DefaultProxyManagerTypeNameProvider{}"/> implementation using MD5 hashes.
        /// </summary>
        public static readonly DefaultProxyManagerTypeNameProvider<Context> Md5TypeNameProvider = (moduleBuilder, proxyInfo)
            => $"{moduleBuilder.FullyQualifiedName}.From<{GetMd5String($"{proxyInfo.Proxy.Context}_{proxyInfo.Proxy.Type.GetBestName()}")}>_To<{GetMd5String($"{proxyInfo.Target.Context}_{proxyInfo.Target.Type.GetBestName()}")}>";

        /// <summary>
        /// The default <see cref="DefaultProxyManagerNoMatchingMethodHandler{}"/> implementation.<br/>
        /// If a method cannot be implemented, <see cref="ArgumentException"/> will be thrown right away.
        /// </summary>
        public static readonly DefaultProxyManagerNoMatchingMethodHandler<Context> ThrowExceptionNoMatchingMethodHandler = (proxyBuilder, proxyInfo, _, _, _, proxyMethod)
            => throw new ArgumentException($"The {proxyInfo.Proxy.Type.GetBestName()} interface defines method {proxyMethod.Name} which doesn't exist in the API.");

        /// <summary>
        /// If a method cannot be implemented, a blank implementation will be created instead, which will throw <see cref="NotImplementedException"/> when called.
        /// </summary>
        public static readonly DefaultProxyManagerNoMatchingMethodHandler<Context> ThrowingImplementationNoMatchingMethodHandler = (proxyBuilder, proxyInfo, _, _, _, proxyMethod) =>
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
            il.Emit(OpCodes.Ldstr, $"The {proxyInfo.Proxy.Type.GetBestName()} interface defines method {proxyMethod.Name} which doesn't exist in the API.");
            il.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor(new Type[] { typeof(string) })!);
            il.Emit(OpCodes.Throw);
        };

        /// <summary>
        /// The type name provider to use.
        /// </summary>
        public readonly DefaultProxyManagerTypeNameProvider<Context> TypeNameProvider;

        /// <summary>
        /// The behavior to use if no matching method to proxy is found.
        /// </summary>
        public readonly DefaultProxyManagerNoMatchingMethodHandler<Context> NoMatchingMethodHandler;

        /// <summary>
        /// The behavior to use when mapping <see cref="Enum"/> arguments while matching methods to proxy.
        /// </summary>
        public readonly DefaultProxyManagerEnumMappingBehavior EnumMappingBehavior;

        /// <summary>
        /// Whether proxy types should implement any marker interfaces.
        /// </summary>
        public readonly ProxyObjectInterfaceMarking ProxyObjectInterfaceMarking;

        /// <summary>
        /// Creates a new configuration for <see cref="DefaultProxyManager{}"/>.
        /// </summary>
        /// <param name="typeNameProvider">The type name provider to use.<br/>Defaults to <see cref="Md5TypeNameProvider"/>.</param>
        /// <param name="noMatchingMethodHandler">The behavior to use if no matching method to proxy is found.<br/>Defaults to <see cref="ThrowExceptionNoMatchingMethodHandler"/>.</param>
        /// <param name="enumMappingBehavior">The behavior to use when mapping <see cref="Enum"/> arguments while matching methods to proxy.<br/>Defaults to <see cref="DefaultProxyManagerEnumMappingBehavior.ThrowAtRuntime"/>.</param>
        /// <param name="proxyObjectInterfaceMarking">Whether proxy types should implement any marker interfaces.<br/>Defaults to <see cref="ProxyObjectInterfaceMarking.Marker"/>.</param>
        public DefaultProxyManagerConfiguration(
            DefaultProxyManagerTypeNameProvider<Context>? typeNameProvider = null,
            DefaultProxyManagerNoMatchingMethodHandler<Context>? noMatchingMethodHandler = null,
            DefaultProxyManagerEnumMappingBehavior enumMappingBehavior = DefaultProxyManagerEnumMappingBehavior.ThrowAtRuntime,
            ProxyObjectInterfaceMarking proxyObjectInterfaceMarking = ProxyObjectInterfaceMarking.Marker
        )
        {
            this.TypeNameProvider = typeNameProvider ?? Md5TypeNameProvider;
            this.NoMatchingMethodHandler = noMatchingMethodHandler ?? ThrowExceptionNoMatchingMethodHandler;
            this.EnumMappingBehavior = enumMappingBehavior;
            this.ProxyObjectInterfaceMarking = proxyObjectInterfaceMarking;
        }
    }

    /// <summary>
    /// The default <see cref="IProxyManager{}"/> implementation.
    /// </summary>
    /// <typeparam name="Context">The context type used to describe the current proxy process. Use <see cref="Nothing"/> if not needed.</typeparam>
    public sealed class DefaultProxyManager<Context>: IProxyManager<Context>
    {
		internal readonly ModuleBuilder ModuleBuilder;
        internal readonly DefaultProxyManagerConfiguration<Context> Configuration;
		private readonly IDictionary<ProxyInfo<Context>, IProxyFactory<Context>> Factories = new Dictionary<ProxyInfo<Context>, IProxyFactory<Context>>();

        /// <summary>
        /// Constructs a <see cref="DefaultProxyManager{}"./>
        /// </summary>
        /// <param name="moduleBuilder">The <see cref="System.Reflection.Emit.ModuleBuilder"/> to use for creating the proxy types in.</param>
        /// <param name="configuration">Configuration to use for this <see cref="DefaultProxyManager{}"/>. Defaults to `null`, which means that the default configuration will be used.</param>
        public DefaultProxyManager(ModuleBuilder moduleBuilder, DefaultProxyManagerConfiguration<Context>? configuration = null)
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
                if (!this.Factories.TryGetValue(proxyInfo, out IProxyFactory<Context>? factory))
                {
                    if (proxyInfo.Target.Type.IsEnum && proxyInfo.Proxy.Type.IsEnum)
                    {
                        factory = new DefaultEnumProxyFactory<Context>(proxyInfo);
                        this.Factories[proxyInfo] = factory;
                    }
                    else if (proxyInfo.Target.Type.IsArray && proxyInfo.Proxy.Type.IsArray)
                    {
                        factory = new DefaultArrayProxyFactory<Context>(proxyInfo);
                        this.Factories[proxyInfo] = factory;
                    }
                    else if (proxyInfo.Proxy.Type.IsInterface || (proxyInfo.Proxy.Type.IsAssignableTo(typeof(Delegate)) && proxyInfo.Target.Type.IsAssignableTo(typeof(Delegate))))
                    {
                        var newFactory = new DefaultProxyFactory<Context>(proxyInfo, this.Configuration.NoMatchingMethodHandler, this.Configuration.EnumMappingBehavior, this.Configuration.ProxyObjectInterfaceMarking);
                        factory = newFactory;
                        this.Factories[proxyInfo] = factory;
                        try
                        {
                            newFactory.Prepare(this, this.Configuration.TypeNameProvider(this.ModuleBuilder, proxyInfo));
                        }
                        catch
                        {
                            this.Factories.Remove(proxyInfo);
                            throw;
                        }
                    }
                    else
                    {
                        var newFactory = new DefaultReconstructingProxyFactory<Context>(proxyInfo, this.Configuration.EnumMappingBehavior);
                        factory = newFactory;
                        this.Factories[proxyInfo] = factory;
                        try
                        {
                            newFactory.Prepare();
                        }
                        catch (Exception e)
                        {
                            this.Factories.Remove(proxyInfo);
                            throw new ArgumentException($"Unhandled proxy/conversion method for info: {proxyInfo}", e);
                        }
                    }
                }
                return factory;
            }
		}
	}
}
