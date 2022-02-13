using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Nanoray.Pintail
{
    public delegate string DefaultProxyManagerTypeNameProvider<Context>(ModuleBuilder moduleBuilder, ProxyInfo<Context> proxyInfo);
    public delegate void DefaultProxyManagerNoMatchingMethodHandler<Context>(TypeBuilder proxyBuilder, ProxyInfo<Context> proxyInfo, FieldBuilder targetField, FieldBuilder glueField, FieldBuilder proxyInfosField, MethodInfo proxyMethod);

    public enum ProxyObjectInterfaceMarking { Disabled, Marker, Property }

    public class DefaultProxyManagerConfiguration<Context>
    {
        public static readonly DefaultProxyManagerTypeNameProvider<Context> DefaultTypeNameProvider = (moduleBuilder, proxyInfo)
            => $"{moduleBuilder.FullyQualifiedName}.From<<{proxyInfo.Proxy.Context}>_<{proxyInfo.Proxy.Type.FullName}>>_To<<{proxyInfo.Target.Context}>_<{proxyInfo.Target.Type.FullName}>>";

        public static readonly DefaultProxyManagerNoMatchingMethodHandler<Context> ThrowExceptionNoMatchingMethodHandler = (proxyBuilder, proxyInfo, _, _, _, proxyMethod)
            => throw new ArgumentException($"The {proxyInfo.Proxy.Type.FullName} interface defines method {proxyMethod.Name} which doesn't exist in the API.");

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
            il.Emit(OpCodes.Ldstr, $"The {proxyInfo.Proxy.Type.FullName} interface defines method {proxyMethod.Name} which doesn't exist in the API.");
            il.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor(new Type[] { typeof(string) })!);
            il.Emit(OpCodes.Throw);
        };

        public DefaultProxyManagerTypeNameProvider<Context> TypeNameProvider { get; set; }
        public DefaultProxyManagerNoMatchingMethodHandler<Context> NoMatchingMethodHandler;
        public ProxyObjectInterfaceMarking ProxyObjectInterfaceMarking { get; }

        public DefaultProxyManagerConfiguration(
            DefaultProxyManagerTypeNameProvider<Context>? typeNameProvider = null,
            DefaultProxyManagerNoMatchingMethodHandler<Context>? noMatchingMethodHandler = null,
            ProxyObjectInterfaceMarking proxyObjectInterfaceMarking = ProxyObjectInterfaceMarking.Marker
        )
        {
            this.TypeNameProvider = typeNameProvider ?? DefaultTypeNameProvider;
            this.NoMatchingMethodHandler = noMatchingMethodHandler ?? ThrowExceptionNoMatchingMethodHandler;
            this.ProxyObjectInterfaceMarking = proxyObjectInterfaceMarking;
        }
    }

    public sealed class DefaultProxyManager<Context>: IProxyManager<Context>
    {
		internal readonly ModuleBuilder ModuleBuilder;
        internal readonly DefaultProxyManagerConfiguration<Context> Configuration;
		private readonly IDictionary<ProxyInfo<Context>, DefaultProxyFactory<Context>> Factories = new Dictionary<ProxyInfo<Context>, DefaultProxyFactory<Context>>();

        public DefaultProxyManager(ModuleBuilder moduleBuilder, DefaultProxyManagerConfiguration<Context>? configuration = null)
		{
			this.ModuleBuilder = moduleBuilder;
            this.Configuration = configuration ?? new();
		}

        public IProxyFactory<Context>? GetProxyFactory(ProxyInfo<Context> proxyInfo)
        {
            lock (this.Factories)
            {
                if (this.Factories.TryGetValue(proxyInfo, out DefaultProxyFactory<Context>? factory))
                    return factory;
            }
            return null;
        }

        public IProxyFactory<Context> ObtainProxyFactory(ProxyInfo<Context> proxyInfo)
		{
			lock (this.Factories)
			{
                if (!this.Factories.TryGetValue(proxyInfo, out DefaultProxyFactory<Context>? factory))
                {
                    factory = new DefaultProxyFactory<Context>(proxyInfo, this.Configuration.NoMatchingMethodHandler, this.Configuration.ProxyObjectInterfaceMarking);
                    this.Factories[proxyInfo] = factory;
                    try
                    {
                        factory.Prepare(this, this.Configuration.TypeNameProvider(this.ModuleBuilder, proxyInfo));
                    }
                    catch
                    {
                        this.Factories.Remove(proxyInfo);
                        throw;
                    }
                }
                return factory;
            }
		}
	}
}
