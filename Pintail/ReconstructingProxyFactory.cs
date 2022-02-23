using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Nanoray.Pintail
{
    internal class ReconstructingProxyFactory<Context>: IProxyFactory<Context>
    {
        public ProxyInfo<Context> ProxyInfo { get; private set; }
        private readonly ProxyManagerEnumMappingBehavior EnumMappingBehavior;
        private Func<IProxyManager<Context>, object, object> ProxyFactory = null!;
        private Func<IProxyManager<Context>, object, object> UnproxyFactory = null!;

        internal ReconstructingProxyFactory(ProxyInfo<Context> proxyInfo, ProxyManagerEnumMappingBehavior enumMappingBehavior)
        {
            this.ProxyInfo = proxyInfo;
            this.EnumMappingBehavior = enumMappingBehavior;
        }

        internal void Prepare()
        {
            Func<IProxyManager<Context>, object, object>? MakeFactory(bool isReverse)
            {
                TypeInfo<Context> from = isReverse ? this.ProxyInfo.Proxy : this.ProxyInfo.Target;
                TypeInfo<Context> to = isReverse ? this.ProxyInfo.Target : this.ProxyInfo.Proxy;

                var deconstructMethod = from.Type.GetMethod("Deconstruct");
                if (deconstructMethod is null || deconstructMethod.ReturnType != typeof(void))
                    return null;
                var deconstructParameters = deconstructMethod.GetParameters();
                if (!deconstructParameters.All(p => p.IsOut))
                    return null;
                var constructor = to.Type.GetConstructors().Where(c => c.GetParameters().Length == deconstructParameters.Length).FirstOrDefault();
                if (constructor is null)
                    return null;
                return (manager, from) =>
                {
                    object?[] callParameters = new object?[deconstructParameters.Length];
                    deconstructMethod.Invoke(from, callParameters);
                    for (int i = 0; i < callParameters.Length; i++)
                    {
                        if (callParameters[i] is null)
                            continue;
                        switch (TypeUtilities.AreTypesMatching(constructor.GetParameters()[i].ParameterType, callParameters[i]!.GetType(), TypeUtilities.MethodTypeMatchingPart.Parameter, this.EnumMappingBehavior))
                        {
                            case TypeUtilities.MatchingTypesResult.True:
                                break;
                            case TypeUtilities.MatchingTypesResult.IfProxied:
                                var unproxyFactory = manager.GetProxyFactory(isReverse ? this.ProxyInfo : this.ProxyInfo.Reversed());
                                if (unproxyFactory is not null && unproxyFactory.TryUnproxy(manager, callParameters[i]!, out object? targetInstance))
                                {
                                    callParameters[i] = targetInstance;
                                    break;
                                }
                                var factory = manager.ObtainProxyFactory(isReverse ? this.ProxyInfo.Reversed() : this.ProxyInfo);
                                callParameters[i] = factory.ObtainProxy(manager, callParameters[i]!);
                                break;
                            case TypeUtilities.MatchingTypesResult.False:
                                throw new InvalidOperationException($"Cannot convert from {from} to type {to.Type}.");
                        }
                    }
                    return constructor.Invoke(null, callParameters)!;
                };
            }

            this.ProxyFactory = MakeFactory(false) ?? throw new ArgumentException("Could not create mapping factory.");
            this.UnproxyFactory = MakeFactory(true) ?? throw new ArgumentException("Could not create mapping factory.");
        }

        public object ObtainProxy(IProxyManager<Context> manager, object targetInstance)
        {
            return this.ProxyFactory(manager, targetInstance);
        }

        public bool TryUnproxy(IProxyManager<Context> manager, object potentialProxyInstance, [NotNullWhen(true)] out object? targetInstance)
        {
            targetInstance = this.UnproxyFactory(manager, potentialProxyInstance);
            return true;
        }
    }
}
