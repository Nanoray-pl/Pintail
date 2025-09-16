using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Nanoray.Pintail
{
    /// <summary>
    /// The default <see cref="IProxyManager{Context}"/> implementation.
    /// </summary>
    /// <typeparam name="Context">The context type used to describe the current proxy process. Use <see cref="Nothing"/> if not needed.</typeparam>
    public sealed class ProxyManager<Context>: IProxyManager<Context>
    {
        internal readonly Func<ProxyInfo<Context>, ModuleBuilder> ModuleBuilderProvider;
        internal readonly ProxyManagerConfiguration<Context> Configuration;
        private readonly Dictionary<ProxyInfo<Context>, IProxyFactory<Context>> Factories = new();
        private readonly ConcurrentDictionary<string, List<Type>> InterfaceMappabilityCache = new();
        private readonly Dictionary<ProxyInfo<Context>, Exception> FailedProxyTypeExceptions = new();

        /// <summary>
        /// Constructs a <see cref="ProxyManager{Context}"/>.
        /// </summary>
        /// <param name="moduleBuilder">The <see cref="System.Reflection.Emit.ModuleBuilder"/> to use for creating the proxy types in.</param>
        /// <param name="configuration">Configuration to use for this <see cref="ProxyManager{Context}"/>. Defaults to `null`, which means that the default configuration will be used.</param>
        public ProxyManager(ModuleBuilder moduleBuilder, ProxyManagerConfiguration<Context>? configuration = null) : this(_ => moduleBuilder, configuration)
        {
        }

        /// <summary>
        /// Constructs a <see cref="ProxyManager{Context}"/>.
        /// </summary>
        /// <param name="moduleBuilderProvider">The <see cref="System.Reflection.Emit.ModuleBuilder"/> to use for creating the proxy types in.</param>
        /// <param name="configuration">Configuration to use for this <see cref="ProxyManager{Context}"/>. Defaults to `null`, which means that the default configuration will be used.</param>
        public ProxyManager(Func<ProxyInfo<Context>, ModuleBuilder> moduleBuilderProvider, ProxyManagerConfiguration<Context>? configuration = null)
        {
            this.ModuleBuilderProvider = moduleBuilderProvider;
            this.Configuration = configuration ?? new();
        }

        internal ModuleBuilder GetModuleBuilder(ProxyInfo<Context> proxyInfo)
        {
            switch (this.Configuration.Synchronization)
            {
                case ProxyManagerSynchronization.None:
                    return this.GetModuleBuilderSync(proxyInfo);
                case ProxyManagerSynchronization.ViaLock:
                    lock (this.Factories)
                        return this.GetModuleBuilderSync(proxyInfo);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private ModuleBuilder GetModuleBuilderSync(ProxyInfo<Context> proxyInfo)
            => this.ModuleBuilderProvider(proxyInfo);

        /// <inheritdoc/>
        public IProxyFactory<Context>? GetProxyFactory(ProxyInfo<Context> proxyInfo)
        {
            switch (this.Configuration.Synchronization)
            {
                case ProxyManagerSynchronization.None:
                    return this.GetProxyFactorySync(proxyInfo);
                case ProxyManagerSynchronization.ViaLock:
                    lock (this.Factories)
                        return this.GetProxyFactorySync(proxyInfo);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IProxyFactory<Context>? GetProxyFactorySync(ProxyInfo<Context> proxyInfo)
            => this.Factories.GetValueOrDefault(proxyInfo);

        /// <inheritdoc/>
        public IProxyFactory<Context> ObtainProxyFactory(ProxyInfo<Context> proxyInfo)
        {
            switch (this.Configuration.Synchronization)
            {
                case ProxyManagerSynchronization.None:
                    return this.ObtainProxyFactorySync(proxyInfo);
                case ProxyManagerSynchronization.ViaLock:
                    lock (this.Factories)
                        return this.ObtainProxyFactorySync(proxyInfo);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IProxyFactory<Context> ObtainProxyFactorySync(ProxyInfo<Context> proxyInfo)
        {
            if (this.FailedProxyTypeExceptions.TryGetValue(proxyInfo, out var priorException))
                throw new ArgumentException($"Unhandled proxy/conversion method for info: {proxyInfo}", priorException);

            if (!this.Factories.TryGetValue(proxyInfo, out var factory))
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
                        this.Configuration.EarlyNoMatchingMethodHandler,
                        this.Configuration.NoMatchingMethodHandler,
                        this.Configuration.ProxyPrepareBehavior,
                        this.Configuration.EnumMappingBehavior,
                        this.Configuration.ProxyObjectInterfaceMarking,
                        this.Configuration.AccessLevelChecking,
                        this.Configuration.Synchronization,
                        this.InterfaceMappabilityCache
                    );
                    factory = newFactory;
                    this.Factories[proxyInfo] = factory;
                    try
                    {
                        newFactory.Prepare(this, this.Configuration.TypeNameProvider(this.GetModuleBuilder(proxyInfo), proxyInfo));
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

        /// <inheritdoc/>
        public IProxyFactory<Context>? TryObtainProxyFactory(ProxyInfo<Context> proxyInfo)
        {
            switch (this.Configuration.Synchronization)
            {
                case ProxyManagerSynchronization.None:
                    return this.TryObtainProxyFactorySync(proxyInfo);
                case ProxyManagerSynchronization.ViaLock:
                    lock (this.Factories)
                        return this.TryObtainProxyFactorySync(proxyInfo);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IProxyFactory<Context>? TryObtainProxyFactorySync(ProxyInfo<Context> proxyInfo)
        {
            if (this.FailedProxyTypeExceptions.ContainsKey(proxyInfo))
                return null;
            return this.ObtainProxyFactory(proxyInfo);
        }
    }
}
