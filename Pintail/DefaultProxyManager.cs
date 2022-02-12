using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Nanoray.Pintail
{
    public struct DefaultProxyManagerConfiguration<Context> where Context : notnull, IEquatable<Context>
    {
        public static readonly Func<ModuleBuilder, ProxyInfo<Context>, string> DefaultTypeNameProvider
            = (moduleBuilder, key) => $"{moduleBuilder.FullyQualifiedName}.From<{key.Proxy.Context}_{key.Proxy.Type.Name}>_To<{key.Target.Context}_{key.Target.Type.Name}>";

        public Func<ModuleBuilder, ProxyInfo<Context>, string> TypeNameProvider { get; set; }
    }

    public sealed class DefaultProxyManager<Context>: IProxyManager<Context> where Context: notnull, IEquatable<Context>
	{
		internal readonly ModuleBuilder ModuleBuilder;
        internal readonly DefaultProxyManagerConfiguration<Context> Configuration;
		private readonly IDictionary<ProxyInfo<Context>, DefaultProxyFactory<Context>> Factories = new Dictionary<ProxyInfo<Context>, DefaultProxyFactory<Context>>();

        public DefaultProxyManager(ModuleBuilder moduleBuilder): this(
            moduleBuilder,
            new DefaultProxyManagerConfiguration<Context> { TypeNameProvider = DefaultProxyManagerConfiguration<Context>.DefaultTypeNameProvider }
        ) { }

        public DefaultProxyManager(ModuleBuilder moduleBuilder, DefaultProxyManagerConfiguration<Context> configuration)
		{
			this.ModuleBuilder = moduleBuilder;
            this.Configuration = configuration;
		}

		public IProxyFactory<Context> ObtainProxyFactory(ProxyInfo<Context> proxyInfo)
		{
			lock (this.Factories)
			{
                if (!this.Factories.TryGetValue(proxyInfo, out DefaultProxyFactory<Context>? factory))
                {
                    factory = new DefaultProxyFactory<Context>(proxyInfo);
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
